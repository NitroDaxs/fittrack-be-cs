using FitTrack.Application.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FitTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
[EnableRateLimiting(RateLimits.Chat)]
public class ChatController(ChatService chat) : ControllerBase
{
    [HttpPost]
    public Task<ChatReplyDto> Ask(ChatRequest request, CancellationToken cancellationToken) =>
        chat.AskAsync(request, User.GetUserId(), cancellationToken);
}