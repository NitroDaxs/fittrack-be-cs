using System.Security.Cryptography;
using FitTrack.Application.Abstractions.Storage;

namespace FitTrack.Application.Services.Admin;

public class AdminUserService(
    IUserRepository users, IExerciseRepository exercises, IUnitOfWork unitOfWork, IPasswordHasher hasher, IAvatarStorage avatars)
{
    public async Task<PagedResult<UserDto>> GetPageAsync(AdminListRequest query, PageRequest paging)
    {
        bool? isActive = query.Status?.ToLowerInvariant() switch
        {
            "active" => true,
            "suspended" or "inactive" => false,
            _ => null,
        };

        var filter = new UserFilter(
            query.Search,
            EnumValues.TryParse<UserRole>(query.Role, out var role) ? role : null,
            isActive);

        var page = await users.GetPageAsync(filter, new SortRequest(query.Sort, query.Dir), paging, defaultPerPage: 20);
        return page.Map(u => u.ToDto());
    }

    public async Task<UserDto> CreateAsync(AdminUserRequest request)
    {
        ValidationFailedException.ThrowIfAny(
            (string.IsNullOrWhiteSpace(request.Name), "name", "The name field is required."),
            (string.IsNullOrWhiteSpace(request.Email), "email", "The email field is required."));

        var email = request.Email!.Trim().ToLowerInvariant();
        if (await users.EmailTakenAsync(email))
        {
            throw new ValidationFailedException("email", "The email has already been taken.");
        }

        var user = new User
        {
            Name = request.Name!,
            Email = email,
            PasswordHash = hasher.Hash(request.Password ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(16))),
            Role = request.Role ?? UserRole.Member,
            IsActive = request.IsActive ?? true,
        };
        users.Add(user);
        await unitOfWork.SaveChangesAsync();

        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(int id, int actingUserId, AdminUserRequest request)
    {
        var user = await users.FindAsync(id)
                   ?? throw new NotFoundException("User not found.");

        if (user.Id == actingUserId && request.Role == UserRole.Member)
        {
            throw new RuleViolationException("You cannot remove your own admin role.");
        }

        if (user.Id == actingUserId && request.IsActive == false)
        {
            throw new RuleViolationException("You cannot deactivate your own account.");
        }

        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await users.EmailTakenAsync(email, exceptId: id))
            {
                throw new ValidationFailedException("email", "The email has already been taken.");
            }
            user.Email = email;
        }

        user.Name = request.Name ?? user.Name;
        user.Role = request.Role ?? user.Role;
        user.IsActive = request.IsActive ?? user.IsActive;
        await unitOfWork.SaveChangesAsync();

        return user.ToDto();
    }

    public Task<UserDto> UpdateRoleAsync(int id, int actingUserId, UserRole? role)
    {
        if (role is null)
        {
            throw new ValidationFailedException("role", "The role field is required.");
        }

        return UpdateAsync(id, actingUserId, new AdminUserRequest { Role = role });
    }

    public async Task<UserDto> ToggleActiveAsync(int id, int actingUserId)
    {
        if (id == actingUserId)
        {
            throw new RuleViolationException("You cannot deactivate your own account.");
        }

        var user = await users.FindAsync(id)
                   ?? throw new NotFoundException("User not found.");

        user.IsActive = !user.IsActive;
        await unitOfWork.SaveChangesAsync();

        return user.ToDto();
    }

    public async Task DeleteAsync(int id, int actingUserId)
    {
        if (id == actingUserId)
        {
            throw new RuleViolationException("You cannot delete your own account.");
        }

        var user = await users.FindAsync(id)
                   ?? throw new NotFoundException("User not found.");

        if (await exercises.AnyCreatedByAsync(id))
        {
            throw new RuleViolationException("This user created exercises in the library. Reassign or delete those first.");
        }

        users.Remove(user);
        await unitOfWork.SaveChangesAsync();

        if (user.AvatarFile is not null)
        {
            avatars.Delete(user.AvatarFile);
        }
    }
}
