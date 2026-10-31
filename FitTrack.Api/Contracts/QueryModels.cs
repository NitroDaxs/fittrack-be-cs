using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Contracts;

// Query-string models: how paging and filters are named on the wire. Services take the
// plain records these convert to, so the Application layer never sees MVC binding attributes.

public class PageQuery
{
    public int Page { get; set; } = 1;
    public int? PerPage { get; set; }

    [FromQuery(Name = "per_page")]
    public int? PerPageSnake { get; set; }

    public PageRequest ToPageRequest() => new(Page, PerPage ?? PerPageSnake);
}

public class ExercisesQuery : PageQuery
{
    public string? Search { get; set; }
    public string? Difficulty { get; set; }

    [FromQuery(Name = "exercise_type")] public string? ExerciseType { get; set; }

    public string? MuscleGroup { get; set; }
    public string? Equipment { get; set; }

    public ExerciseSearch ToSearch() => new(Search, Difficulty, ExerciseType, MuscleGroup, Equipment);
}

public class AdminListQuery : PageQuery
{
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public string? Dir { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public string? Goal { get; set; }
    public string? Level { get; set; }
    public string? Author { get; set; }

    public AdminListRequest ToRequest() => new(Search, Sort, Dir, Category, Status, Role, Goal, Level, Author);
}
