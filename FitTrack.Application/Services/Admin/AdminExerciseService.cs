namespace FitTrack.Application.Services.Admin;

public class AdminExerciseService(IExerciseRepository exercises, ITaxonomyRepository taxonomies, IUnitOfWork unitOfWork)
{
    public async Task<PagedResult<ExerciseDto>> GetPageAsync(AdminListRequest query, PageRequest paging)
    {
        var filter = new ExerciseFilter(
            query.Search,
            ExerciseType: EnumValues.TryParse<ExerciseCategory>(query.Category, out var category) ? category : null);

        var page = await exercises.GetPageAsync(filter, new SortRequest(query.Sort, query.Dir), paging, defaultPerPage: 20);
        return page.Map(e => e.ToDto());
    }

    public async Task<ExerciseDto> CreateAsync(int userId, ExerciseRequest request)
    {
        ValidationFailedException.ThrowIfAny(
            (string.IsNullOrWhiteSpace(request.Name), "name", "The name field is required."),
            (request.Difficulty is null, "difficulty", "The difficulty field is required."),
            (request.ExerciseType is null, "exercise_type", "The exercise type field is required."));

        var exercise = new Exercise
        {
            Name = request.Name!,
            Slug = Slug.From(request.Name!),
            Description = request.Description,
            Instructions = request.Instructions,
            Difficulty = request.Difficulty!.Value,
            ExerciseType = request.ExerciseType!.Value,
            VideoUrl = request.VideoUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            IsPublished = request.IsPublished ?? true,
            CreatedBy = userId,
        };

        if (await exercises.SlugTakenAsync(exercise.Slug))
        {
            throw new ValidationFailedException("name", "An exercise with this name already exists.");
        }

        await SyncRelationsAsync(exercise, request);
        exercises.Add(exercise);
        await unitOfWork.SaveChangesAsync();

        return (await exercises.GetWithDetailsAsync(exercise.Id)).ToDto();
    }

    public async Task<ExerciseDto> UpdateAsync(int id, ExerciseRequest request)
    {
        var exercise = await exercises.FindForUpdateAsync(id)
                       ?? throw new NotFoundException("Exercise not found.");

        if (request.Name is not null)
        {
            var slug = Slug.From(request.Name);
            if (await exercises.SlugTakenAsync(slug, exceptId: id))
            {
                throw new ValidationFailedException("name", "An exercise with this name already exists.");
            }

            exercise.Name = request.Name;
            exercise.Slug = slug;
        }
        exercise.Description = request.Description ?? exercise.Description;
        exercise.Instructions = request.Instructions ?? exercise.Instructions;
        exercise.Difficulty = request.Difficulty ?? exercise.Difficulty;
        exercise.ExerciseType = request.ExerciseType ?? exercise.ExerciseType;
        exercise.VideoUrl = request.VideoUrl ?? exercise.VideoUrl;
        exercise.ThumbnailUrl = request.ThumbnailUrl ?? exercise.ThumbnailUrl;
        exercise.IsPublished = request.IsPublished ?? exercise.IsPublished;

        await SyncRelationsAsync(exercise, request);
        await unitOfWork.SaveChangesAsync();

        return (await exercises.GetWithDetailsAsync(id)).ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        if (await exercises.DeleteAsync(id) == 0)
        {
            throw new NotFoundException("Exercise not found.");
        }
    }

    private async Task SyncRelationsAsync(Exercise exercise, ExerciseRequest request)
    {
        if (request.MuscleGroups is not null)
        {
            var slugs = request.MuscleGroups.Select(Slug.From).ToList();
            var primary = (request.PrimaryMuscles ?? []).Select(Slug.From).ToHashSet();
            var groups = await taxonomies.GetMuscleGroupsBySlugAsync(slugs);

            var wanted = groups.ToDictionary(
                g => g.Id,
                g => primary.Contains(g.Slug) ? MuscleRole.Primary : MuscleRole.Secondary);

            exercise.MuscleGroups.RemoveAll(link => !wanted.ContainsKey(link.MuscleGroupId));
            foreach (var link in exercise.MuscleGroups)
            {
                link.Role = wanted[link.MuscleGroupId];
            }
            foreach (var (groupId, role) in wanted)
            {
                if (exercise.MuscleGroups.All(link => link.MuscleGroupId != groupId))
                {
                    exercise.MuscleGroups.Add(new ExerciseMuscleGroup { MuscleGroupId = groupId, Role = role });
                }
            }
        }

        if (request.Equipment is not null)
        {
            var slugs = request.Equipment.Select(Slug.From).ToList();
            var ids = await taxonomies.GetEquipmentIdsBySlugAsync(slugs);

            exercise.Equipment.RemoveAll(link => !ids.Contains(link.EquipmentId));
            foreach (var equipmentId in ids.Where(i => exercise.Equipment.All(link => link.EquipmentId != i)))
            {
                exercise.Equipment.Add(new ExerciseEquipment { EquipmentId = equipmentId });
            }
        }
    }
}
