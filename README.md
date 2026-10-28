# FitTrack API in C#

A rebuild of the Laravel FitTrack API (`../fitness-backend`) in ASP.NET Core 10,
Entity Framework Core and PostgreSQL. It serves the same routes and JSON, so the
React app (`../../fittrack`) works against it unchanged.

Step-by-step guide: https://claude.ai/artifact/YWbTMsSDTSBBHxnRBxboCn

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
