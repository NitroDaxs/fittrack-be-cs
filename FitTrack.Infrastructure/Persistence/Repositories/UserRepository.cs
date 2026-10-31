using System.Linq.Expressions;

namespace FitTrack.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    private static readonly Dictionary<string, Expression<Func<User, object>>> Sortable = new()
    {
        ["name"] = u => u.Name,
        ["email"] = u => u.Email,
        ["role"] = u => u.Role,
        ["created_at"] = u => u.CreatedAt,
        ["joined"] = u => u.CreatedAt,
        ["status"] = u => u.IsActive,
    };

    public Task<PagedResult<User>> GetPageAsync(UserFilter filter, SortRequest sort, PageRequest paging, int defaultPerPage)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = filter.Search.ToSearchPattern();
            users = users.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        if (filter.Role is { } role)
        {
            users = users.Where(u => u.Role == role);
        }

        if (filter.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        return users
            .SortBy(sort, Sortable, fallback: "created_at")
            .PaginateAsync(paging, defaultPerPage);
    }

    public Task<User> GetAsync(int id) => db.Users.AsNoTracking().FirstAsync(u => u.Id == id);

    public Task<User> GetForUpdateAsync(int id) => db.Users.FirstAsync(u => u.Id == id);

    public async Task<User?> FindAsync(int id) => await db.Users.FindAsync(id);

    public Task<User?> FindByEmailAsync(string email) => db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> EmailTakenAsync(string email, int? exceptId = null) =>
        db.Users.AnyAsync(u => u.Email == email && (exceptId == null || u.Id != exceptId));

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);
}
