namespace FitTrack.Application.Abstractions.Persistence;

public record UserFilter(string? Search, UserRole? Role, bool? IsActive);

public interface IUserRepository
{
    Task<PagedResult<User>> GetPageAsync(UserFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage);

    /// <summary>Read-only; throws if the user does not exist.</summary>
    Task<User> GetAsync(int id);

    /// <summary>Change-tracked; throws if the user does not exist.</summary>
    Task<User> GetForUpdateAsync(int id);

    /// <summary>Change-tracked.</summary>
    Task<User?> FindAsync(int id);

    Task<User?> FindByEmailAsync(string email);
    Task<bool> EmailTakenAsync(string email, int? exceptId = null);
    void Add(User user);
    void Remove(User user);
}
