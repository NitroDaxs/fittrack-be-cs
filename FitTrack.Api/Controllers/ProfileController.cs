using FitTrack.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class ProfileController(ProfileService profile, AvatarService avatars) : ControllerBase
{
    [HttpGet]
    public Task<UserDto> Show() => profile.GetAsync(User.GetUserId());

    [HttpPut]
    public Task<UserDto> Update(UpdateProfileRequest request) => profile.UpdateAsync(User.GetUserId(), request);

    [EnableRateLimiting(RateLimits.Auth)]
    [HttpPut("password")]
    public Task<AuthResponse> ChangePassword(ChangePasswordRequest request) =>
        profile.ChangePasswordAsync(User.GetUserId(), request);

    [EnableRateLimiting(RateLimits.Uploads)]
    [RequestSizeLimit(RejectOversizedAvatarAttribute.MaxRequestBytes)]
    [RejectOversizedAvatar]
    [HttpPost("avatar")]
    public async Task<UserDto> UploadAvatar(IFormFile file)
    {
        await using var content = file.OpenReadStream();
        return await avatars.ReplaceAsync(User.GetUserId(), content, file.Length);
    }

    [HttpDelete("avatar")]
    public Task<UserDto> RemoveAvatar() => avatars.RemoveAsync(User.GetUserId());

    [HttpGet("favorites")]
    [HttpGet("saved")]
    public Task<FavoritesDto> Favorites() => profile.GetFavoritesAsync(User.GetUserId());
}
