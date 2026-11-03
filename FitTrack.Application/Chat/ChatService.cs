using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FitTrack.Application.Chat;

/// <summary>
/// Answers one chat message. The tool loop itself runs in the chat client's function invocation
/// middleware, which Infrastructure sets up; this class builds the conversation and the tools.
/// </summary>
public class ChatService(
    IEnumerable<IChatTool> tools,
    IChatInstructions instructions,
    ILogger<ChatService> logger,
    IChatClient? chatClient = null)
{
    private const int MaxHistoryTurns = 12;
    private const int MaxTurnLength = 4000;

    public async Task<ChatReplyDto> AskAsync(ChatRequest request, int userId,
        CancellationToken cancellationToken = default)
    {
        if (chatClient is null)
        {
            throw new ChatUnavailableException("The assistant isn't set up on this server.");
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, await instructions.BuildAsync(userId, request.Language))
        };

        messages.AddRange(History(request.History));
        messages.Add(new ChatMessage(ChatRole.User, request.Message!.Trim()));

        var context = new ToolContext(userId, new ChatCards());
        var options = new ChatOptions
        {
            Tools = [.. tools.Select(tool => new ChatToolFunction(tool, context, logger))],
            ToolMode = ChatToolMode.Auto,
        };

        var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);

        logger.LogInformation(
            "Answered with {Calls} model call(s): {Prompt} prompt + {Completion} completion tokens",
            response.Messages.Count(m => m.Role == ChatRole.Assistant),
            response.Usage?.InputTokenCount ?? 0, response.Usage?.OutputTokenCount ?? 0);

        // Only the last assistant message is the answer; earlier ones belong to tool rounds.
        var text = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text;
        return new ChatReplyDto(Reply(text, request.Language), context.Cards.ToDto());
    }

    private static IEnumerable<ChatMessage> History(List<ChatTurn> turns) =>
        turns
            .Where(t => !string.IsNullOrWhiteSpace(t.Content) && t.Role is "user" or "assistant")
            .TakeLast(MaxHistoryTurns)
            .Select(t =>
            {
                var text = t.Content.Length <= MaxTurnLength ? t.Content : t.Content[..MaxTurnLength];
                return new ChatMessage(t.Role == "user" ? ChatRole.User : ChatRole.Assistant, text);
            });

    private static string Reply(string? text, string? language) =>
        string.IsNullOrWhiteSpace(text)
            ? language == "bg"
                ? "Съжалявам, не успях да съставя отговор. Можеш ли да го зададеш по друг начин?"
                : "Sorry, I couldn't put an answer together. Could you ask that another way?"
            : text.Trim();
}
