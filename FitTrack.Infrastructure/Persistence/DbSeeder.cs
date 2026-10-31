using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitTrack.Infrastructure.Persistence;

public class DbSeeder(AppDbContext db, IHostEnvironment env, ILogger<DbSeeder> logger)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private record ExerciseSeed(
        string Name, string Video, string Difficulty, string Type,
        List<string> Equipment, List<string> Primary, List<string> Secondary,
        string Description, List<string> Instructions);

    private record RoutineSeed(string Title, string Description, int DaysPerWeek, string Goal, string Level, List<string> Workouts);

    private record ArticleSeed(string Title, string Category, JsonElement Body);

    public async Task SeedAsync()
    {
        if (await db.Users.AnyAsync())
        {
            logger.LogWarning("The database already has users, so nothing was seeded. Reset it with: dotnet ef database drop");
            return;
        }
        
        var folder = Path.Combine(env.ContentRootPath, "..", "seed-data");

        var admin = new User
        {
            Name = "Admin User",
            Email = "admin@test.com",
            Role = UserRole.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
        };
        var member = new User
        {
            Name = "John Member",
            Email = "member@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
        };
        db.Users.AddRange(admin, member);

        var exercises = SeedExercises(Read<ExerciseSeed>(folder, "exercises.json"), admin);
        var routines = SeedRoutines(Read<RoutineSeed>(folder, "routines.json"), admin, exercises);
        SeedArticles(Read<ArticleSeed>(folder, "articles.json"), admin);
        SeedMemberHistory(member, exercises, routines);
        
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Exercises} exercises, {Routines} routines and 2 users.", exercises.Count, routines.Count);
    }

    private static List<T> Read<T>(string folder, string file) =>
        JsonSerializer.Deserialize<List<T>>(File.ReadAllText(Path.Combine(folder, file)), Json)
        ?? throw new InvalidOperationException($"{file} is empty.");

    private List<Exercise> SeedExercises(List<ExerciseSeed> seeds, User admin)
    {
        // Create each muscle group and piece of equipment once, keyed by name.
        var muscles = seeds.SelectMany(s => s.Primary.Concat(s.Secondary)).Distinct()
            .ToDictionary(name => name, name => new MuscleGroup { Name = name, Slug = Slug.From(name) });
        var equipment = seeds.SelectMany(s => s.Equipment).Distinct()
            .ToDictionary(name => name, name => new Equipment { Name = name, Slug = Slug.From(name) });

        var exercises = seeds.Select(seed => new Exercise
        {
            Name = seed.Name,
            Slug = Slug.From(seed.Name),
            Description = seed.Description,
            Instructions = string.Join("\n", seed.Instructions.Select((step, i) => $"{i + 1}. {step}")),
            Difficulty = EnumValues.Parse<Difficulty>(seed.Difficulty),
            ExerciseType = EnumValues.Parse<ExerciseCategory>(seed.Type),
            VideoUrl = $"https://www.youtube.com/watch?v={seed.Video}",
            ThumbnailUrl = $"https://i.ytimg.com/vi/{seed.Video}/hqdefault.jpg",
            Creator = admin,
            MuscleGroups =
            [
                .. seed.Primary.Select(name => new ExerciseMuscleGroup { MuscleGroup = muscles[name], Role = MuscleRole.Primary }),
                .. seed.Secondary.Select(name => new ExerciseMuscleGroup { MuscleGroup = muscles[name], Role = MuscleRole.Secondary }),
            ],
            Equipment = seed.Equipment.Select(name => new ExerciseEquipment { Equipment = equipment[name] }).ToList(),
            Media = [new ExerciseMedia { Type = MediaType.Video, Url = $"https://www.youtube.com/embed/{seed.Video}" }],
        }).ToList();

        db.Exercises.AddRange(exercises);
        return exercises;
    }

    private List<Routine> SeedRoutines(List<RoutineSeed> seeds, User admin, List<Exercise> exercises)
    {
        var random = new Random(42); // fixed seed: the same "random" picks on every run

        var routines = seeds.Select(seed => new Routine
        {
            Title = seed.Title,
            Slug = Slug.From(seed.Title),
            Description = seed.Description,
            DaysPerWeek = seed.DaysPerWeek,
            Goal = EnumValues.Parse<RoutineGoal>(seed.Goal),
            Level = EnumValues.Parse<Difficulty>(seed.Level),
            IsPublic = true,
            Creator = admin,
            Workouts = seed.Workouts.Select((name, day) =>
            {
                var picks = exercises.ToArray();
                random.Shuffle(picks);

                return new Workout
                {
                    Name = name,
                    DayOrder = day + 1,
                    Creator = admin,
                    WorkoutExercises = picks.Take(5).Select((exercise, i) => new WorkoutExercise
                    {
                        Exercise = exercise,
                        Order = i + 1,
                        TargetSets = 4,
                        TargetReps = "8-12",
                        TargetWeight = 60,
                        RestSeconds = 90,
                        Notes = "Keep form strict",
                    }).ToList(),
                };
            }).ToList(),
        }).ToList();

        db.Routines.AddRange(routines);
        return routines;
    }

    private void SeedArticles(List<ArticleSeed> seeds, User author)
    {
        db.Articles.AddRange(seeds.Select((seed, i) => new Article
        {
            Title = seed.Title,
            Slug = Slug.From(seed.Title),
            Body = seed.Body.GetRawText(),
            Category = EnumValues.Parse<ArticleCategory>(seed.Category),
            Status = ArticleStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-7 * i),
            Author = author,
        }));
    }

    private void SeedMemberHistory(User member, List<Exercise> exercises, List<Routine> routines)
    {
        var plan = new (Exercise Exercise, decimal Top, decimal StepPerWeek, int Reps)[]
        {
            (exercises[0], 190, 4, 8),
            (exercises[1], 285, 6, 6),
            (exercises[2], 335, 7, 5),
            (exercises[3], 0, 0, 10),
            (exercises[4], 100, 2, 10),
        };

        List<int> daysAgo = [0, 1, 2, 3];
        for (var d = 6; d <= 63; d += 2)
        {
            daysAgo.Add(d);
        }

        string[] names = ["Push Day", "Pull Day", "Leg Day", "Upper Body", "Full Body"];
        var today = DateTime.UtcNow.Date;

        for (var i = 0; i < daysAgo.Count; i++)
        {
            var log = new WorkoutLog
            {
                User = member,
                Name = names[i % names.Length],
                Date = today.AddDays(-daysAgo[i]).AddHours(18),
                DurationMinutes = 45 + i % 4 * 10,
            };

            var weeksAgo = daysAgo[i] / 7m;
            foreach (var (exercise, top, step, reps) in new[] { plan[0], plan[1 + i % 4] })
            {
                var weight = top == 0
                    ? 0
                    : Math.Max(45, Math.Round((top - weeksAgo * step) / 5, MidpointRounding.AwayFromZero) * 5);

                for (var set = 1; set <= 3; set++)
                {
                    log.SetLogs.Add(new SetLog
                    {
                        Exercise = exercise,
                        SetNumber = set,
                        Weight = weight,
                        Reps = set == 3 ? reps - 1 : reps,
                    });
                }
            }

            db.WorkoutLogs.Add(log);
        }

        for (var i = 0; i < 8; i++)
        {
            db.BodyMeasurements.Add(new BodyMeasurement
            {
                User = member,
                Date = DateOnly.FromDateTime(today.AddDays(-9 * i)),
                Weight = 86 + i * 0.9m,
                BodyFatPct = 12 + i * 0.5m,
                CustomMeasurements = new CustomMeasurements
                {
                    Chest = 42 - i * 0.2m,
                    Waist = 32 + i * 0.3m,
                    Arms = 15.5m - i * 0.1m,
                },
            });
        }

        db.SavedExercises.AddRange(exercises.Take(3).Select(e => new SavedExercise { User = member, Exercise = e }));
        db.SavedRoutines.Add(new SavedRoutine { User = member, Routine = routines[0] });
    }
}