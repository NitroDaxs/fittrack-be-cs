namespace FitTrack.Application.Services;

public class RoutineService(IRoutineRepository routines)
{
    public async Task<PagedResult<RoutineDto>> GetPageAsync(RoutineSearch search, PageRequest paging)
    {
        int? exactDays = int.TryParse(search.Days, out var parsedDays) ? parsedDays : null;
        var fiveOrMore = search.Days is "5" or "5+";

        var filter = new RoutineFilter(
            search.Search,
            ParseAll<RoutineGoal>(search.Goals),
            ParseAll<Difficulty>(search.Levels),
            DaysPerWeek: fiveOrMore ? null : exactDays,
            MinDaysPerWeek: fiveOrMore ? 5 : null);

        var page = await routines.GetPublicPageAsync(filter, paging, defaultPerPage: 8);
        return page.Map(r => r.ToDto());
    }

    public async Task<RoutineDto> GetAsync(string key)
    {
        var routine = await routines.FindPublicWithProgrammeAsync(key)
                      ?? throw new NotFoundException("Routine not found.");

        return routine.ToDto(withProgramme: true);
    }

    private static List<TEnum> ParseAll<TEnum>(IEnumerable<string> values) where TEnum : struct, Enum =>
        values
            .Select(v => EnumValues.TryParse<TEnum>(v, out var parsed) ? parsed : (TEnum?)null)
            .OfType<TEnum>()
            .ToList();
}
