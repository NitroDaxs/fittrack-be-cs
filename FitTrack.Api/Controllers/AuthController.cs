using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthController(AuthService auth, PasswordResetService passwordReset) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request));

    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request) => auth.LoginAsync(request);

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        await passwordReset.RequestAsync(request);
        return Ok(new { message = "If an account exists for that email, we've sent a password reset link." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        await passwordReset.ResetAsync(request);
        return Ok(new { message = "Your password has been reset." });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new { message = "You have been logged out." });
}
