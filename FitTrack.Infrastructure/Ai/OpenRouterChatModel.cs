using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FitTrack.Application.Abstractions.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FitTrack.Infrastructure.Ai;

public class OpenRouterChatModel(HttpClient http, IOptions<OpenRouterOptions> options, ILogger<OpenRouterChatModel> logger)
    : IChatModel
{
    private readonly OpenRouterOptions _settings = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.ApiKey) && !string.IsNullOrWhiteSpace(_settings.Model);

    public async Task<ChatModelReply> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        bool allowToolCalls,
        CancellationToken cancellationToken)
    {
        var body = BuildRequest(messages, tools, allowToolCalls);
        logger.LogDebug("OpenRouter request: {Body}", body.ToJsonString());

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Headers.Add("HTTP-Referer", _settings.AppUrl);
        request.Headers.Add("X-Title", _settings.AppTitle);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("OpenRouter didn't answer within {Timeout}.", http.Timeout);
            throw new ChatUnavailableException("The assistant took too long to answer. Please try again.");
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Couldn't reach OpenRouter.");
            throw new ChatUnavailableException("The assistant can't be reached right now. Please try again later.");
        }

        using (response)
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogDebug("OpenRouter response {Status}: {Body}", (int)response.StatusCode, json);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("OpenRouter returned {Status}: {Body}", (int)response.StatusCode, json);
                throw new ChatUnavailableException(response.StatusCode switch
                {
                    HttpStatusCode.PaymentRequired => "The assistant has run out of credit for now.",
                    HttpStatusCode.TooManyRequests => "The assistant is busy. Please try again in a minute.",
                    _ => "The assistant couldn't answer right now. Please try again.",
                });
            }

            return ParseReply(json);
        }
    }

    private JsonObject BuildRequest(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools, bool allowToolCalls)
    {
        var body = new JsonObject
        {
            ["model"] = _settings.Model,
            ["messages"] = new JsonArray(messages.Select(ToJson).ToArray()),
            ["temperature"] = _settings.Temperature,
            ["max_tokens"] = _settings.MaxTokens,
        };

        if (tools.Count > 0)
        {
            body["tools"] = new JsonArray(tools.Select(ToJson).ToArray());
            body["tool_choice"] = allowToolCalls ? "auto" : "none";
        }

        return body;
    }

    private static JsonNode ToJson(ChatMessage message)
    {
        var json = new JsonObject
        {
            ["role"] = message.Role switch
            {
                ChatRole.System => "system",
                ChatRole.User => "user",
                ChatRole.Assistant => "assistant",
                _ => "tool",
            },
            ["content"] = message.Content,
        };

        if (message.ToolCalls is { Count: > 0 } calls)
        {
            json["tool_calls"] = new JsonArray(calls.Select(call => (JsonNode)new JsonObject
            {
                ["id"] = call.Id,
                ["type"] = "function",
                ["function"] = new JsonObject { ["name"] = call.Name, ["arguments"] = call.ArgumentsJson },
            }).ToArray());
        }

        if (message.ToolCallId is not null)
        {
            json["tool_call_id"] = message.ToolCallId;
        }

        return json;
    }

    private static JsonNode ToJson(ToolDefinition tool) => new JsonObject
    {
        ["type"] = "function",
        ["function"] = new JsonObject
        {
            ["name"] = tool.Name,
            ["description"] = tool.Description,
            ["parameters"] = JsonNode.Parse(tool.Parameters.GetRawText()),
        },
    };

    private ChatModelReply ParseReply(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // A provider can fail after OpenRouter has answered 200; the error then sits in the body.
        if (root.TryGetProperty("error", out var error)
            || (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                && choices[0].TryGetProperty("error", out error)))
        {
            logger.LogError("OpenRouter reported an error: {Error}", error.GetRawText());
            throw new ChatUnavailableException("The assistant couldn't answer right now. Please try again.");
        }

        var message = root.GetProperty("choices")[0].GetProperty("message");

        var text = message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String
            ? content.GetString()
            : null;

        var toolCalls = new List<ToolCall>();
        if (message.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in calls.EnumerateArray())
            {
                var function = call.GetProperty("function");
                var arguments = function.TryGetProperty("arguments", out var args)
                    ? args.ValueKind == JsonValueKind.String ? args.GetString() ?? "{}" : args.GetRawText()
                    : "{}";

                toolCalls.Add(new ToolCall(
                    call.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                    function.GetProperty("name").GetString() ?? "",
                    arguments));
            }
        }

        return new ChatModelReply(text, toolCalls, ParseUsage(root));
    }

    private static ChatUsage? ParseUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object) return null;

        int Tokens(string name) => usage.TryGetProperty(name, out var value) && value.TryGetInt32(out var n) ? n : 0;

        decimal? cost = usage.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number
            ? decimal.Parse(c.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture)
            : null;

        return new ChatUsage(Tokens("prompt_tokens"), Tokens("completion_tokens"), cost);
    }
}