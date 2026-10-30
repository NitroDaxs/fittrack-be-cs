using System.Text.Json;

namespace FitTrack.Api.Dtos;

public static class Mappings
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.Units, user.IsActive, user.CreatedAt);

    public static TaxonomyDto ToDto(this MuscleGroup group) => new(group.Id, group.Name, group.Slug);

    public static TaxonomyDto ToDto(this Equipment equipment) => new(equipment.Id, equipment.Name, equipment.Slug);

    public static ExerciseDto ToDto(this Exercise e) => new(
        e.Id, e.Name, e.Slug, e.Description, e.Instructions, e.Difficulty, e.ExerciseType,
        e.VideoUrl, e.ThumbnailUrl, e.CreatedBy, e.IsPublished, e.CreatedAt, e.UpdatedAt,
        e.MuscleGroups
            .Select(l => new MuscleGroupDto(l.MuscleGroup.Id, l.MuscleGroup.Name, l.MuscleGroup.Slug, new PivotDto(l.Role)))
            .ToList(),
        e.Equipment.Select(l => l.Equipment.ToDto()).ToList(),
        e.Media.OrderBy(m => m.SortOrder).Select(m => new MediaDto(m.Id, m.Type, m.Url, m.SortOrder)).ToList());

    public static RoutineDto ToDto(this Routine r, bool withProgramme = false) =>
        new(r.Id, r.Title, r.Slug, r.Description, r.Goal, r.Level, r.DaysPerWeek, r.IsPublic, r.IsPublished,
            r.CreatedAt, r.Creator is null ? null : new CreatorDto(r.Creator.Id, r.Creator.Name))
        {
            Workouts = withProgramme ? r.Workouts.OrderBy(w => w.DayOrder).Select(ToDto).ToList() : null,
        };

    public static WorkoutDto ToDto(this Workout w) =>
        new(w.Id, w.Name, w.DayOrder, w.WorkoutExercises.OrderBy(x => x.Order).Select(ToDto).ToList());

    public static WorkoutExerciseDto ToDto(this WorkoutExercise x) =>
        new(x.Id, x.ExerciseId, x.Order, x.TargetSets, x.TargetReps, x.TargetWeight, x.RestSeconds, x.Notes, x.Exercise.ToDto());

    public static WorkoutLogDto ToDto(this WorkoutLog log) =>
        new(log.Id, log.UserId, log.WorkoutId, log.Name, log.Date, log.DurationMinutes, log.Notes, log.CreatedAt,
            log.SetLogs.OrderBy(s => s.Id)
                .Select(s => new SetLogDto(s.Id, s.ExerciseId, s.SetNumber, s.Weight, s.Reps, s.IsCompleted, s.Exercise.ToDto()))
                .ToList());

    public static BodyMeasurementDto ToDto(this BodyMeasurement m) =>
        new(m.Id, m.Date, m.Weight, m.BodyFatPct, m.CustomMeasurements, m.PhotoUrl, m.CreatedAt);

    public static ArticleDto ToDto(this Article a)
    {
        var plain = PlainText(Blocks(a.Body));
        var words = plain.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return new ArticleDto(
            a.Id, a.Slug, a.Title, a.Category, a.Status,
            Excerpt: plain.Length <= 160 ? plain : plain[..160].TrimEnd() + "...",
            Author: a.Author?.Name ?? "FitTrack",
            Date: DateOnly.FromDateTime(a.PublishedAt ?? a.CreatedAt),
            ReadMinutes: Math.Max(1, (int)Math.Ceiling(words / 200.0)),
            Image: "");
    }

    public static JsonElement Blocks(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement.Clone();
            }
        }
        catch (JsonException)
        {
            
        }

        return JsonSerializer.SerializeToElement(new[] { new { type = "p", text = body } });
    }

    private static string PlainText(JsonElement blocks) =>
        string.Join(' ', blocks.EnumerateArray()
            .Where(b => b.ValueKind == JsonValueKind.Object
                && b.TryGetProperty("text", out var text)
                && text.ValueKind == JsonValueKind.String)
            .Select(b => b.GetProperty("text").GetString()));
}