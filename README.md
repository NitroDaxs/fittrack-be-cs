# FitTrack API

The backend for FitTrack, a fitness tracking app with an exercise library, training
routines, workout logging, body measurements, fitness calculators and an admin
console. It is a REST API built with ASP.NET Core 10, Entity Framework Core and
PostgreSQL.

The React app that uses it lives in the
[fittrack-frontend](https://github.com/NitroDaxs/fittrack-frontend) repository.

## Contents

- [Tech stack](#tech-stack)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [Architecture](#architecture)
- [API reference](#api-reference)
- [Calculators](#calculators)
- [Authentication](#authentication)
- [Password reset](#password-reset)
- [Database](#database)
- [Testing](#testing)
- [Deployment](#deployment)
- [Known limitations](#known-limitations)

## Tech stack

| Area | Choice |
| --- | --- |
| Runtime | .NET 10, ASP.NET Core controllers |
| Database | PostgreSQL 17, EF Core with Npgsql, snake_case naming |
| Auth | JWT bearer tokens, BCrypt password hashing |
| Email | [Resend](https://resend.com) |
| API docs | OpenAPI and Scalar (Development only) |
| Tests | xUnit |

## Getting started

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download), Docker (or your
own PostgreSQL), and the EF Core tool:

```bash
dotnet tool install --global dotnet-ef
```

**1. Start PostgreSQL.** These values match the default connection string:

```bash
docker run -d --name fittrack-db -p 5432:5432 \
  -e POSTGRES_PASSWORD=fittrack -e POSTGRES_DB=fittrack postgres:17
```

**2. Create the tables:**

```bash
dotnet ef database update --project FitTrack.Infrastructure --startup-project FitTrack.Api
```

**3. Load the sample data.** This does nothing if the database already has users:

```bash
dotnet run --project FitTrack.Api -- --seed
```

**4. Start the API:**

```bash
dotnet run --project FitTrack.Api
```

The API runs at http://localhost:8000, with interactive docs at
http://localhost:8000/scalar/v1 and the OpenAPI document at
http://localhost:8000/openapi/v1.json.

### Sample accounts

The seed creates two local development accounts, along with 302 exercises, 3
routines and 6 articles:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@test.com` | `password123` |
| Member | `member@test.com` | `password123` |

Never seed a production database without changing these passwords.

## Configuration

Settings come from `FitTrack.Api/appsettings.json`, then
`appsettings.Development.json`, user secrets (in Development) and environment
variables, with later sources winning. In environment variables, `:` becomes `__`,
for example `Jwt__Key`.

| Setting | Purpose | Default |
| --- | --- | --- |
| `ConnectionStrings:Default` | PostgreSQL connection. If empty, the API uses `DATABASE_URL_UNPOOLED`, then `DATABASE_URL` | Local `fittrack` database in Development; none otherwise, and the app won't start without one |
| `Database:MigrateOnStartup` | Apply migrations at startup, and seed if there are no users | `false` |
| `Jwt:Key` | Token signing key, at least 32 characters | Development value only; the app won't start without one |
| `Jwt:Issuer` | Token issuer | `fittrack-api` |
| `Jwt:Audience` | Token audience | `fittrack-web` |
| `Jwt:ExpiresMinutes` | Token lifetime | `10080` (7 days) |
| `Email:ResendApiKey` | Resend API key | Empty, so no emails are sent |
| `Email:From` | Sender of account emails | `FitTrack <onboarding@resend.dev>` |
| `Email:FrontendUrl` | Base URL for links in emails | `http://localhost:5173` |
| `Storage:UploadsPath` | Folder for uploaded pictures, relative to the API project | `uploads`; `/tmp/uploads` in Production |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API | `http://localhost:5173` |

### Secrets

`appsettings.Development.json` is tracked by Git, so never put real secrets in it.
Keep the Resend key in .NET user secrets when developing:

```bash
dotnet user-secrets set "Email:ResendApiKey" "<your-key>" --project FitTrack.Api
```

Resend's `onboarding@resend.dev` sender only delivers to the address on your own
Resend account. To email anyone else, verify a domain with Resend and change
`Email:From`.

## Architecture

The solution follows the Onion architecture: references only ever point inwards.
`FitTrack.Tests/ArchitectureTests.cs` fails if that stops being true.

| Project | Holds | References |
| --- | --- | --- |
| `FitTrack.Domain` | Entities and enums | Nothing |
| `FitTrack.Application` | Business logic, DTOs, and the interfaces it needs (`Abstractions/`) | Domain |
| `FitTrack.Infrastructure` | EF Core, repositories, migrations, seeder, JWT, BCrypt, Resend | Application |
| `FitTrack.Api` | Controllers, query-string models, error responses, startup | Application, Infrastructure |

```
FitTrack.Api/
  Controllers/        HTTP endpoints; Admin/ holds the admin console's
  Contracts/          Query-string models
  Http/               Error responses and exception handling
  Program.cs          Startup, and the only Api code that uses Infrastructure types
FitTrack.Application/
  Abstractions/       Interfaces for persistence, email, security and storage
  Common/             Exceptions, paging, slugs
  Dtos/               Request and response shapes
  Services/           Business logic; Admin/ holds the admin console's
FitTrack.Domain/
  Entities/           Database entities and enums
FitTrack.Infrastructure/
  Email/              Resend sender
  Persistence/        DbContext, repositories, migrations, seeder
  Security/           JWT and BCrypt
  Storage/            Disk usage for the admin dashboard
FitTrack.Tests/       Unit and architecture tests
seed-data/            JSON files the seeder reads
```

A request goes from a controller to an Application service, which uses repositories
through their interfaces. Business rules belong in Application services, not in
controllers or the frontend.

### Adding an endpoint

1. Add request and response records in `FitTrack.Application/Dtos/`.
2. Put the logic in a service in `FitTrack.Application/Services/`, and register it
   in that project's `DependencyInjection.cs`.
3. If it needs new data access, declare an interface in `Abstractions/` and implement
   it in `FitTrack.Infrastructure`, registering it in that project's
   `DependencyInjection.cs`.
4. Add a thin controller action in `FitTrack.Api/Controllers/`.
5. Add tests in `FitTrack.Tests`.

## API reference

All routes start with `/api`. Use the Scalar page for full request and response
schemas.

**Access:** Public needs no token, Member needs a valid token, and Admin needs a
token with the `admin` role.

### Auth

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/register` | Public | Create an account; returns 201 |
| POST | `/login` | Public | Get a token |
| POST | `/forgot-password` | Public | Request a reset email |
| POST | `/reset-password` | Public | Set a new password with a reset token |
| POST | `/logout` | Member | Confirm logout (see [Authentication](#authentication)) |

### Catalog

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/exercises` | Public | Paged list |
| GET | `/exercises/{key}` | Public | One exercise |
| GET | `/routines` | Public | Paged list |
| GET | `/routines/{key}` | Public | One routine with its programme |
| GET | `/articles` | Public | Paged list |
| GET | `/articles/{key}` | Public | One article |
| GET | `/taxonomies` | Public | Filter and form options |

### Calculators

| Method | Path | Access |
| --- | --- | --- |
| POST | `/calculators/one-rep-max` | Public |
| POST | `/calculators/bmi` | Public |
| POST | `/calculators/tdee` | Public |
| POST | `/calculators/macros` | Public |
| POST | `/calculators/body-fat` | Public |

### Member

| Method | Path | Purpose |
| --- | --- | --- |
| GET, PUT | `/me` | Read or update the name, email and units; changing the email needs `current_password` |
| POST, DELETE | `/me/avatar` | Upload (multipart field `file`) or remove the profile picture |
| PUT | `/me/password` | Change the password and get a new token; ends other sessions and is rate limited |
| GET | `/me/saved` | Saved exercises and routines (`/me/favorites` is an alias) |
| GET | `/me/dashboard` | Dashboard data |
| GET | `/me/exercises/{id}/progression` | Strength history for one exercise |
| GET, POST | `/me/workouts` | Paged workout history, or log a workout |
| GET | `/me/workouts/{id}` | One logged workout |
| GET, POST | `/me/measurements` | Read or add body measurements |
| POST, DELETE | `/exercises/{id}/save` | Save or unsave an exercise |
| POST, DELETE | `/routines/{id}/save` | Save or unsave a routine |

### Admin

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/admin/stats` | Dashboard figures |
| GET | `/admin/activity` | Recent activity |
| GET, POST | `/admin/exercises` | List or create |
| PUT, DELETE | `/admin/exercises/{id}` | Update or delete |
| GET, POST | `/admin/routines` | List or create |
| GET, PUT, DELETE | `/admin/routines/{id}` | Read, update or delete |
| GET, POST | `/admin/articles` | List or create |
| PUT, DELETE | `/admin/articles/{id}` | Update or delete |
| GET, POST | `/admin/users` | List or create |
| PUT, DELETE | `/admin/users/{id}` | Update or delete |
| PATCH | `/admin/users/{id}/role` | Change a user's role |
| PATCH | `/admin/users/{id}/toggle-active` | Suspend or reactivate a user |

### Conventions

JSON properties and enum values use snake_case, such as `height_cm` and
`normal_weight`.

**Lists** accept `page` and `per_page`, and return:

```json
{
  "data": [],
  "current_page": 1,
  "per_page": 20,
  "total": 302,
  "last_page": 16,
  "from": 1,
  "to": 20
}
```

The numbers above are illustrative. These filters are available as well:

| Route | Query parameters |
| --- | --- |
| `/exercises` | `search`, `difficulty`, `exercise_type`, `muscleGroup` and `equipment` (comma-separated to match any) |
| `/routines` | `search`, `goals`, `levels`, `daysPerWeek` |
| `/admin/*` lists | `search`, `sort`, `dir`, `category`, `status`, `role`, `goal`, `level`, `author` |

**Errors** use these status codes:

| Status | Meaning |
| --- | --- |
| 401 | Missing, invalid or expired token |
| 403 | Signed in, but without the required role |
| 404 | Not found; the body has a `message` |
| 422 | Validation failed or a business rule was broken |

A validation error names each invalid field:

```json
{
  "message": "Enter between 1 and 20 reps.",
  "errors": { "reps": ["Enter between 1 and 20 reps."] }
}
```

## Calculators

All calculator logic lives in `FitTrack.Application/Services/CalculatorService.cs`.
The frontend only collects inputs and displays results. These endpoints don't store
anything or need a token.

| Endpoint | Method | Request fields | Result fields |
| --- | --- | --- | --- |
| `one-rep-max` | Epley | `weight`, `reps` (1 to 20) | `one_rep_max`, `low_accuracy`, `zones` |
| `bmi` | Standard BMI | `units`, height, `weight` | `bmi`, `category` |
| `tdee` | Mifflin-St Jeor | `units`, height, `weight`, `sex`, `age`, `activity` | `tdee`, `bmr`, `activity_calories`, `cut`, `lean_bulk` |
| `macros` | Percentage split | `calories`, `split` | `calories`, `targets` |
| `body-fat` | US Navy | `units`, height, `sex`, `neck`, `waist`, and `hip` for women | `body_fat`, `category` |

- **Units:** `units` is `metric` or `imperial`. Metric uses kg and cm; imperial uses
  lb and inches.
- **Height:** send `height_cm` for metric, or `height_feet` and `height_inches` for
  imperial.
- **Activity:** `sedentary`, `light`, `moderate`, `heavy` or `athlete`.
- **Split:** `balanced`, `high_protein`, `low_carb` or `keto`.

Example:

```bash
curl -X POST http://localhost:8000/api/calculators/bmi \
  -H "Content-Type: application/json" \
  -d '{"units":"metric","weight":70,"height_cm":175}'
```

```json
{ "bmi": 22.9, "category": "normal_weight" }
```

## Authentication

`POST /login` and `POST /register` return the user and a token:

```json
{
  "user": { "id": 1, "name": "Admin User", "email": "admin@test.com", "role": "admin" },
  "access_token": "<jwt>",
  "token_type": "Bearer"
}
```

Send it with each request as `Authorization: Bearer <jwt>`.

Each authenticated request also checks the token against the account. Suspending or
deleting a user, or changing their password, ends their sessions straight away, and
role changes apply on the next request. `PUT /me/password` returns a new token, so the
person changing their password stays logged in.

`POST /logout` doesn't revoke the token. The client logs out by discarding it, and a
copy of that token would stay valid until it expires.

Endpoints need a signed-in user by default, so a public one must opt out with
`[AllowAnonymous]`. Register, login, forgot password, reset password and change password
allow 10 requests per minute from each IP address, then return 429.

## Password reset

1. `POST /forgot-password` always returns the same message, so it can't reveal which
   emails have accounts.
2. For an active account, the API stores a SHA-256 hash of a random token and emails
   a link to `{Email:FrontendUrl}` through Resend. Only one request per account is
   honoured each minute.
3. `POST /reset-password` sets the new password. The token lasts 60 minutes and works
   once.

Without `Email:ResendApiKey`, the API logs an error and sends nothing, but the
endpoint still returns its normal response. Check the API log if an email doesn't
arrive.

## Profile pictures

Pictures can be JPEG, PNG or WebP, up to 2 MB, with up to 10 uploads per minute from
each IP address. The API checks the file's contents rather
than its name, stores it under a random name, and serves it publicly from
`/uploads/avatars`. Replacing a picture or deleting the account deletes the old file.

Files are stored by `LocalAvatarStorage` behind the `IAvatarStorage` interface, so cloud
storage only needs a new implementation of it.

## Database

Migrations live in `FitTrack.Infrastructure/Persistence/Migrations`. The API only
applies them on startup when `Database:MigrateOnStartup` is `true`.

```bash
# Add a migration
dotnet ef migrations add <Name> --project FitTrack.Infrastructure --startup-project FitTrack.Api --output-dir Persistence/Migrations

# Apply migrations
dotnet ef database update --project FitTrack.Infrastructure --startup-project FitTrack.Api
```

If a running API has locked the build output, stop it first, or add
`--configuration Release` to both the build and the EF command.

### Seed data

| File | Contents |
| --- | --- |
| `seed-data/exercises.json` | 302 exercises with muscles, equipment, video ids and steps |
| `seed-data/articles.json` | 6 articles as content blocks |
| `seed-data/routines.json` | 3 routines and their day names |

The first entries in `exercises.json` are the core lifts, in the order the sample
member's training history expects, so keep them first.

## Testing

```bash
dotnet test
```

The tests cover the calculators, password reset, workout streaks and the architecture
rules. They run without a database. Endpoints and repositories have no automated
tests.

## Deployment

1. Set at least `ConnectionStrings__Default`, `Jwt__Key`, `Email__ResendApiKey`,
   `Email__From`, `Email__FrontendUrl` and `Cors__AllowedOrigins__0` as environment
   variables. Generate a new `Jwt__Key` rather than reusing the development one.
2. Apply migrations as a release step.
3. Keep `Storage:UploadsPath` on a persistent disk and back it up. On hosts with
   temporary disks, uploaded pictures are lost on each deploy unless you do this.
4. Serve the API over HTTPS. The app doesn't redirect to HTTPS itself, so do this at
   your host or reverse proxy.

The docs pages at `/scalar/v1` and `/openapi/v1.json` are only available in
Development.

## Deploy to Vercel

The API runs on Vercel's Container Runtime (beta, available on Hobby) from the
`Dockerfile` at the repo root, with a Neon database from the Vercel Marketplace.

1. Import the repository into a new Vercel project. Vercel detects the `Dockerfile` and
   selects the Container preset, so no `vercel.json` is needed.
2. Add the Neon integration from the Marketplace and connect it to the project. Leave
   its custom prefix empty, because the API reads the unprefixed variable names.
3. Set the environment variables below, then deploy.
4. **Change the seeded passwords straight away.** On an empty database the first start
   seeds `admin@test.com` (an admin) and `member@test.com`, both with the password
   `password123`, which is public in this repository.

| Variable | Value |
| --- | --- |
| `Database__MigrateOnStartup` | `true` |
| `Jwt__Key` | 64 or more random characters, for example from `openssl rand -base64 48` |
| `Cors__AllowedOrigins__0` | The React site's URL, such as `https://fittrack.example.com`, with no trailing slash |
| `Email__FrontendUrl` | The React site's URL |
| `Email__ResendApiKey` | Optional. Without it, no emails are sent |

The Neon integration supplies `DATABASE_URL_UNPOOLED` and `DATABASE_URL`, so don't set
`ConnectionStrings__Default`. The API prefers the unpooled URL because migrations don't
work through Neon's PgBouncer pooler.

Vercel routes to port 80 unless you set `PORT`, and the container listens on `PORT`,
defaulting to 80. The image also turns on `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, so rate
limits use the client IP from `X-Forwarded-For`. Vercel
[overwrites that header](https://vercel.com/docs/headers/request-headers#x-forwarded-for),
so clients can't spoof it there. Anywhere else, only run this image behind a proxy that
does the same.

What to expect on Vercel:

- Uploaded pictures are stored in `/tmp/uploads`, so they belong to one instance and are
  lost when it scales down. Profiles then show a broken image link until a new picture
  is uploaded.
- Vercel rejects request bodies over 4.5 MB with its own 413 error. The API's 2 MB
  avatar limit sits well below that.
- Rate limits are counted per instance, so several instances allow proportionally more
  attempts.
- Each instance opens direct database connections, because it uses the unpooled URL.

To try the image locally against a database that is reachable from the container:

```bash
docker build -t fittrack-api .
docker run --rm -p 8080:8080 -e PORT=8080 -e Database__MigrateOnStartup=true \
  -e Jwt__Key="$(openssl rand -base64 48)" \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=<empty-db>;Username=postgres;Password=fittrack" \
  fittrack-api
```

## Known limitations

- Logging out doesn't revoke the token on the server.
- Outside the Docker image, rate limits use the connecting IP address, so behind a
  reverse proxy all visitors share one limit unless forwarded headers are set up.
- Uploaded pictures keep their original metadata, which can include the location a
  photo was taken.
- Progress-photo upload and social sign-in aren't implemented.
