namespace FitTrack.Application.Services;

public class ProfileService(
    IUserRepository users,
    IFavoritesRepository favorites,
    IPasswordResetTokenRepository resetTokens,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    ITokenService tokens)
{
    public async Task<UserDto> GetAsync(int userId) => (await users.GetAsync(userId)).ToDto();

    public async Task<UserDto> UpdateAsync(int userId, UpdateProfileRequest request)
    {
        var user = await users.GetForUpdateAsync(userId);

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name.Length == 0)
            {
                throw new ValidationFailedException("name", "Enter your name.");
            }

            user.Name = name;
        }

        if (request.Email is not null)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (email != user.Email)
            {
                await ChangeEmailAsync(user, email, request.CurrentPassword);
            }
        }

        user.Units = request.Units ?? user.Units;

        await unitOfWork.SaveChangesAsync();
        return user.ToDto();
    }

    /// <summary>
    /// Ends the account's other sessions, and returns a new token so that the
    /// person making the change stays logged in.
    /// </summary>
    public async Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await users.GetForUpdateAsync(userId);
        RequireCurrentPassword(user, request.CurrentPassword);

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await resetTokens.DeleteForUserAsync(user.Id);
        await unitOfWork.SaveChangesAsync();

        return new AuthResponse(user.ToDto(), tokens.CreateToken(user));
    }

    public async Task<FavoritesDto> GetFavoritesAsync(int userId)
    {
        var routines = await favorites.GetSavedRoutinesAsync(userId);
        var exercises = await favorites.GetSavedExercisesAsync(userId);

        return new FavoritesDto(
            routines.Select(r => r.ToDto()).ToList(),
            exercises.Select(e => e.ToDto()).ToList());
    }

    // The email can reset the password, so changing it needs the same proof, and
    // any reset link already sent to the old address stops working.
    private async Task ChangeEmailAsync(User user, string email, string? currentPassword)
    {
        RequireCurrentPassword(user, currentPassword);

        if (await users.EmailTakenAsync(email, exceptId: user.Id))
        {
            throw new ValidationFailedException("email", "The email address is already used.");
        }

        user.Email = email;
        await resetTokens.DeleteForUserAsync(user.Id);
    }

    private void RequireCurrentPassword(User user, string? currentPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || !hasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new ValidationFailedException("current_password", "The current password is incorrect.");
        }
    }
}
