# FitTrack API in C#

A rebuild of the Laravel FitTrack API (`../fitness-backend`) in ASP.NET Core 10,
Entity Framework Core and PostgreSQL. It serves the same routes and JSON, so the
React app (`../../fittrack`) works against it unchanged.

Step-by-step guide: https://claude.ai/artifact/YWbTMsSDTSBBHxnRBxboCn

## Architecture

The solution follows the Onion architecture, so references only ever point inwards.
`FitTrack.Tests/ArchitectureTests.cs` fails the build's tests if that stops being true.

| Project | Holds | References |
| --- | --- | --- |
| `FitTrack.Domain` | Entities and enums | nothing |
| `FitTrack.Application` | Services, DTOs, and the ports they need (`Abstractions/`) | Domain |
| `FitTrack.Infrastructure` | EF Core, repositories, migrations, seeder, JWT, BCrypt | Application |
| `FitTrack.Api` | Controllers, query-string models, error responses, composition root | Application, Infrastructure |

Services throw `NotFoundException`, `RuleViolationException` or
`ValidationFailedException`; `ApplicationExceptionFilter` turns them into 404 and
422 responses. `Program.cs` is the only Api code that uses Infrastructure types.

Migrations now live in Infrastructure, so EF commands name both projects:

```powershell
dotnet ef migrations add <Name> --project FitTrack.Infrastructure --startup-project FitTrack.Api --output-dir Persistence/Migrations
dotnet ef database update --project FitTrack.Infrastructure --startup-project FitTrack.Api
```

## What's here so far

`seed-data/` holds the Laravel seed data exported to JSON, which the C# seeder
reads in step 9 of the guide:

| File | Contents | From |
| --- | --- | --- |
| `exercises.json` | 302 exercises with muscles, equipment, video ids and steps | `database/seeders/data/exercises/*.php` |
| `articles.json` | 6 articles as content blocks | `database/seeders/data/articles.php` |
| `routines.json` | 3 routines and their day names | `RoutineSeeder.php` (the duplicated "Upper Lower Power" entries collapsed to one) |

The first exercises in `exercises.json` are the core lifts, in the order the
member training history expects. Keep them first.

## Start

Follow the guide from step 2. Step 3 creates the solution in this folder:

```powershell
dotnet new sln -n FitTrack
dotnet new webapi -n FitTrack.Api --use-controllers
```
