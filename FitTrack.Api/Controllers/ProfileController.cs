using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class ProfileController(ProfileService profile) : ControllerBase
{
    [HttpGet]
    public Task<UserDto> Show() => profile.GetAsync(User.GetUserId());

    [HttpPut]
    public Task<UserDto> Update(UpdateProfileRequest request) => profile.UpdateAsync(User.GetUserId(), request);

    [EnableRateLimiting(RateLimits.Auth)]
    [HttpPut("password")]
    public Task<AuthResponse> ChangePassword(ChangePasswordRequest request) =>
        profile.ChangePasswordAsync(User.GetUserId(), request);

    [HttpGet("favorites")]
    [HttpGet("saved")]
    public Task<FavoritesDto> Favorites() => profile.GetFavoritesAsync(User.GetUserId());
}
