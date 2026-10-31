namespace FitTrack.Application.Services;

public class ProfileService(
    IUserRepository users, IFavoritesRepository favorites, IUnitOfWork unitOfWork, IPasswordHasher hasher)
{
    public async Task<UserDto> GetAsync(int userId) => (await users.GetAsync(userId)).ToDto();

    public async Task<UserDto> UpdateAsync(int userId, UpdateProfileRequest request)
    {
        var user = await users.GetForUpdateAsync(userId);

        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await users.EmailTakenAsync(email, exceptId: userId))
            {
                throw new ValidationFailedException("email", "The email address is already used");
            }

            user.Email = email;
        }

        if (request.NewPassword is not null)
        {
            if (request.CurrentPassword is null || !hasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new RuleViolationException("Current password is incorrect");
            }

            user.PasswordHash = hasher.Hash(request.NewPassword);
        }

        user.Name = request.Name ?? user.Name;
        user.Units = request.Units ?? user.Units;

        await unitOfWork.SaveChangesAsync();
        return user.ToDto();
    }

    public async Task<FavoritesDto> GetFavoritesAsync(int userId)
    {
        var routines = await favorites.GetSavedRoutinesAsync(userId);
        var exercises = await favorites.GetSavedExercisesAsync(userId);

        return new FavoritesDto(
            routines.Select(r => r.ToDto()).ToList(),
            exercises.Select(e => e.ToDto()).ToList());
    }
}
