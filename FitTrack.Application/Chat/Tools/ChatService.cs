using System.Diagnostics;
using System.Text.Json;
using Fittrack.Application.Chat;
using Microsoft.Extensions.Logging;

namespace FitTrack.Application.Chat.Tools;

public class ChatService(
    IChatModel model,
    IEnumerable<IChatTool> tools,
    IChatInstructions instructions,
    ILogger<ChatService> logger)
{
    private const int MaxToolRounds = 5;

    private const int MaxHistoryTurns = 12;
    private const int MaxTurnLength = 4000;

    private readonly Dictionary<string, IChatTool> _tools = tools.ToDictionary(tool => tool.Name);

    public async Task<ChatResponse> AskAsync(ChatRequest request, int userId,
        CancellationToken cancellationToken = default)
    {
        if (!model.IsConfigured)
        {
            throw new ChatUnavailableException("The assistant isn't set up on this server.");
        }

        var messages = new List<ChatMessage>
        {
            ChatMessage.Instructions(await instructions.BuildAsync(userId, request.Language))
        };

        messages.AddRange(History(request.History));
        messages.Add(ChatMessage.FromUser(request.Message!.Trim()));

        var definitions = _tools.Values.Select(t => new ToolDefinition(t.Name, t.Description, t.Parameters)).ToList();
        var context = new ToolContext(userId, new ChatCards());
        var usage = new List<ChatUsage>();

        for (var round = 1; round <= MaxToolRounds; round++)
        {
            var reply = await model.CompleteAsync(messages, definitions, allowToolCalls: true, cancellationToken);
            if (reply.Usage is not null) usage.Add(reply.Usage);

            if (reply.ToolCalls.Count == 0)
            {
                return Finish(reply.Text, context, usage, request.Language);
            }

            messages.Add(ChatMessage.FromAssistant(reply.Text, reply.ToolCalls));

            foreach (var call in reply.ToolCalls)
            {
                var result = await RunToolAsync(call, context);
                logger.LogInformation("Round {Round}: {Tool}({Arguments}) -> {Result}",
                    round, call.Name, call.ArgumentsJson, Preview(result));
                messages.Add(ChatMessage.ToolResult(call.Id, result));
            }
        }

        logger.LogWarning("Reached {Max} tool rounds; asking for a final answer.", MaxToolRounds);
        var last = await model.CompleteAsync(messages, definitions, allowToolCalls: false, cancellationToken);
        if (last.Usage is not null) usage.Add(last.Usage);

        return Finish(last.Text, context, usage, request.Language);
    }

    private async Task<string> RunToolAsync(ToolCall call, ToolContext context)
    {
        if (!_tools.TryGetValue(call.Name, out var tool))
        {
            return ToolJson.Error($"There is no tool called \"{call.Name}\".");
        }

        JsonElement arguments;
        try
        {
            using var document =
                JsonDocument.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson);
            arguments = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return ToolJson.Error("The arguments were not valid JSON.");
        }

        try
        {
            return ToolJson.Serialize(await tool.RunAsync(new ToolArguments(arguments), context));
        }
        catch (ToolArgumentException e)
        {
            return ToolJson.Error(e.Message);
        }
        catch (NotFoundException e)
        {
            return ToolJson.Error(e.Message);
        }
        catch (ValidationFailedException e)
        {
            return ToolJson.Error(string.Join(" ", e.Errors.Values));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "The {Tool} tool failed.", call.Name);
            return ToolJson.Error(
                "The tool failed. Try different arguments, or tell the person you couldn't look it up.");
        }
    }

    private static IEnumerable<ChatMessage> History(List<ChatTurn> turns) =>
        turns
            .Where(t => !string.IsNullOrWhiteSpace(t.Content) && t.Role is "user" or "assistant")
            .TakeLast(MaxHistoryTurns)
            .Select(t =>
            {
                var text = t.Content.Length <= MaxTurnLength ? t.Content : t.Content[..MaxTurnLength];
                return t.Role == "user" ? ChatMessage.FromUser(text) : ChatMessage.FromAssistant(text);
            });

    private ChatResponse Finish(string? text, ToolContext context, List<ChatUsage> usage, string? language)
    {
        logger.LogInformation(
            "Answered with {Calls} model call(s): {Prompt} prompt + {Completion} completion tokens, ${Cost}",
            usage.Count, usage.Sum(u => u.PromptTokens), usage.Sum(u => u.CompletionTokens),
            usage.Sum(u => u.CostUsd ?? 0));

        var reply = string.IsNullOrWhiteSpace(text)
            ? language == "bg"
                ? "Съжалявам, не успях да съставя отговор. Можеш ли да го зададеш по друг начин?"
                : "Sorry, I couldn't put an answer together. Could you ask that another way?"
            : text.Trim();

        return new ChatResponse(reply, context.Cards.ToDto());
    }

    private static string Preview(string json) => json.Length <= 300 ? json : json[..300] + "...";
}