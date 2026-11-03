using System.Text.Json;

namespace FitTrack.Application.Abstractions.Ai;

public enum ChatRole
{
    System,
    User,
    Assistant,
    Tool
}

public record ToolCall(string Id, string Name, string ArgumentsJson);

public record ChatMessage(
    ChatRole Role,
    string? Content,
    IReadOnlyList<ToolCall>? ToolCalls = null,
    string? ToolCallId = null)
{
    public static ChatMessage Instructions(string text) => new(ChatRole.System, text);

    public static ChatMessage FromUser(string text) => new(ChatRole.User, text);

    public static ChatMessage FromAssistant(string? text, IReadOnlyList<ToolCall>? toolCalls = null) =>
        new(ChatRole.Assistant, text, toolCalls);

    public static ChatMessage ToolResult(string toolCallId, string json) =>
        new(ChatRole.Tool, json, ToolCallId: toolCallId);
}

public record ToolDefinition(string Name, string Description, JsonElement Parameters);

public record ChatUsage(int PromptTokens, int CompletionTokens, decimal? CostUsd);

public record ChatModelReply(string? Text, IReadOnlyList<ToolCall> ToolCalls, ChatUsage? Usage = null);

public interface IChatModel
{
    bool IsConfigured { get; }

    Task<ChatModelReply> CompleteAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools,
        bool allowToolCalls, CancellationToken cancellationToken);
}