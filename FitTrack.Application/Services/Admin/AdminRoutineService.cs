namespace FitTrack.Application.Services.Admin;

public class AdminRoutineService(IRoutineRepository routines, IExerciseRepository exercises, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<RoutineDto>> GetPageAsync(AdminListRequest query, PageRequest paging)
    {
        var filter = new RoutineFilter(
            query.Search,
            EnumValues.TryParse<RoutineGoal>(query.Goal, out var goal) ? [goal] : [],
            EnumValues.TryParse<Difficulty>(query.Level, out var level) ? [level] : []);

        var page = await routines.GetPageAsync(filter, new SortRequest(query.Sort, query.Dir), paging, defaultPerPage: 20);
        return page.Map(r => r.ToDto());
    }

    public async Task<RoutineDto> GetAsync(int id)
    {
        var routine = await routines.FindWithProgrammeAsync(id)
                      ?? throw new NotFoundException("Routine not found.");

        return routine.ToDto(withProgramme: true);
    }

    public async Task<RoutineDto> CreateAsync(int userId, RoutineRequest request)
    {
        ValidationFailedException.ThrowIfAny(
            (string.IsNullOrWhiteSpace(request.Title), "title", "The title field is required."),
            (request.Goal is null, "goal", "The goal field is required."),
            (request.Level is null, "level", "The level field is required."),
            (request.DaysPerWeek is null, "days_per_week", "The days per week field is required."));

        var routine = new Routine
        {
            Title = request.Title!,
            Slug = Slug.From(request.Title!),
            Description = request.Description,
            Goal = request.Goal!.Value,
            Level = request.Level!.Value,
            DaysPerWeek = request.DaysPerWeek!.Value,
            IsPublic = request.IsPublic ?? true,
            CreatedBy = userId,
        };

        if (await routines.SlugTakenAsync(routine.Slug))
        {
            throw new ValidationFailedException("title", "A routine with this title already exists.");
        }

        if (request.Days is not null)
        {
            await ReplaceDaysAsync(routine, request.Days, userId);
        }

        routines.Add(routine);
        await unitOfWork.SaveChangesAsync();

        return await GetAsync(routine.Id);
    }

    public async Task<RoutineDto> UpdateAsync(int id, int userId, RoutineRequest request)
    {
        var routine = await routines.FindForUpdateAsync(id)
                      ?? throw new NotFoundException("Routine not found.");

        if (request.Title is not null)
        {
            var slug = Slug.From(request.Title);
            if (await routines.SlugTakenAsync(slug, exceptId: id))
            {
                throw new ValidationFailedException("title", "A routine with this title already exists.");
            }

            routine.Title = request.Title;
            routine.Slug = slug;
        }
        routine.Description = request.Description ?? routine.Description;
        routine.Goal = request.Goal ?? routine.Goal;
        routine.Level = request.Level ?? routine.Level;
        routine.DaysPerWeek = request.DaysPerWeek ?? routine.DaysPerWeek;
        routine.IsPublic = request.IsPublic ?? routine.IsPublic;

        if (request.Days is not null)
        {
            await ReplaceDaysAsync(routine, request.Days, userId);
        }

        await unitOfWork.SaveChangesAsync();
        return await GetAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        if (await routines.DeleteAsync(id) == 0)
        {
            throw new NotFoundException("Routine not found.");
        }
    }

    private async Task ReplaceDaysAsync(Routine routine, List<RoutineDayRequest> days, int userId)
    {
        var exerciseIds = days.SelectMany(d => d.Exercises).Select(e => e.ExerciseId!.Value).Distinct().ToList();
        var knownIds = await exercises.GetExistingIdsAsync(exerciseIds);
        var unknownIds = exerciseIds.Except(knownIds).ToList();
        if (unknownIds.Count > 0)
        {
            throw new ValidationFailedException("days", $"Unknown exercise id(s): {string.Join(", ", unknownIds)}.");
        }

        routines.ReplaceWorkouts(routine, days.Select((day, dayIndex) => new Workout
        {
            Name = day.Name,
            DayOrder = dayIndex + 1,
            CreatedBy = userId,
            WorkoutExercises = day.Exercises.Select((entry, i) => new WorkoutExercise
            {
                ExerciseId = entry.ExerciseId!.Value,
                Order = i + 1,
                TargetSets = entry.TargetSets,
                TargetReps = entry.TargetReps,
                TargetWeight = entry.TargetWeight,
                RestSeconds = entry.RestSeconds,
                Notes = entry.Notes,
            }).ToList(),
        }));
    }
}
