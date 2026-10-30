using FitTrack.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return ApiErrors.Field("email", "The email has already been taken.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        
        return StatusCode(StatusCodes.Status201Created, new AuthResponse(user.ToDto(), tokens.CreateToken(user)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ApiErrors.Field("email", "The provided credentials are incorrect.");
        }

        if (!user.IsActive)
        {
            return ApiErrors.Field("email", "This account has been suspended.");
        }
        
        return Ok(new AuthResponse(user.ToDto(), tokens.CreateToken(user)));
    }
    
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new {message = "You have been logged out."});
}