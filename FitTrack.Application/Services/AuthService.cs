namespace FitTrack.Application.Services;

public class AuthService(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher hasher, ITokenService tokens)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.EmailTakenAsync(email))
        {
            throw new ValidationFailedException("email", "The email has already been taken.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
        };
        users.Add(user);
        await unitOfWork.SaveChangesAsync();

        return new AuthResponse(user.ToDto(), tokens.CreateToken(user));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.FindByEmailAsync(email);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationFailedException("email", "The provided credentials are incorrect.");
        }

        if (!user.IsActive)
        {
            throw new ValidationFailedException("email", "This account has been suspended.");
        }

        return new AuthResponse(user.ToDto(), tokens.CreateToken(user));
    }
}
