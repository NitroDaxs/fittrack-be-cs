using System.Linq.Expressions;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/admin/users")]
public class AdminUsersController(AppDbContext db) : ControllerBase
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
    
    [HttpGet]
    public async Task<PagedResult<UserDto>> Index([FromQuery] AdminListQuery query)
    {
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            users = users.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        if (EnumValues.TryParse<UserRole>(query.Role, out var role))
        {
            users = users.Where(u => u.Role == role);
        }

        users = query.Status?.ToLowerInvariant() switch
        {
            "active" => users.Where(u => u.IsActive),
            "suspended" or "inactive" => users.Where(u => !u.IsActive),
            _ => users,
        };

        return await users
            .SortBy(query.Sort, query.Dir, Sortable, fallback: "created_at")
            .PaginateAsync(query, defaultPerPage: 20, u => u.ToDto());
    }
    
    [HttpPost]
    public async Task<IActionResult> Store(AdminUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) ModelState.AddModelError("name", "The name field is required.");
        if (string.IsNullOrWhiteSpace(request.Email)) ModelState.AddModelError("email", "The email field is required.");
        if (!ModelState.IsValid) return ApiErrors.Validation(ModelState);

        var email = request.Email!.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return ApiErrors.Field("email", "The email has already been taken.");
        }

        var user = new User
        {
            Name = request.Name!,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(16))),
            Role = request.Role ?? UserRole.Member,
            IsActive = request.IsActive ?? true,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, user.ToDto());
    }
    
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AdminUserRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (user.Id == User.GetUserId() && request.Role == UserRole.Member)
        {
            return ApiErrors.Message("You cannot remove your own admin role.");
        }

        if (user.Id == User.GetUserId() && request.IsActive == false)
        {
            return ApiErrors.Message("You cannot deactivate your own account.");
        }

        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email && u.Id != id))
            {
                return ApiErrors.Field("email", "The email has already been taken.");
            }
            user.Email = email;
        }

        user.Name = request.Name ?? user.Name;
        user.Role = request.Role ?? user.Role;
        user.IsActive = request.IsActive ?? user.IsActive;
        await db.SaveChangesAsync();

        return Ok(user.ToDto());
    }
    
    [HttpPatch("{id:int}/role")]
    public Task<IActionResult> UpdateRole(int id, AdminUserRequest request) =>
        request.Role is null
            ? Task.FromResult<IActionResult>(ApiErrors.Field("role", "The role field is required."))
            : Update(id, new AdminUserRequest { Role = request.Role });
    
    [HttpPatch("{id:int}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        if (id == User.GetUserId())
        {
            return ApiErrors.Message("You cannot deactivate your own account.");
        }

        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        return Ok(user.ToDto());
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Destroy(int id)
    {
        if (id == User.GetUserId())
        {
            return ApiErrors.Message("You cannot delete your own account.");
        }

        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (await db.Exercises.AnyAsync(e => e.CreatedBy == id))
        {
            return ApiErrors.Message("This user created exercises in the library. Reassign or delete those first.");
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Ok(new { status = "deleted" });
    }
}