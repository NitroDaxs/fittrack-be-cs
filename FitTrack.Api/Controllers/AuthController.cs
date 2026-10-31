using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Api.Controllers;

[ApiController]
[Route("api")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request));

    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request) => auth.LoginAsync(request);

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => Ok(new { message = "You have been logged out." });
}
