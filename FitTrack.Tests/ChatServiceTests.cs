using System.Text.Json;
using FitTrack.Application.Chat;
using FitTrack.Application.Chat.Tools;
using FitTrack.Application.Common;
using FitTrack.Application.Dtos;
using FitTrack.Application.Services;
using FitTrack.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitTrack.Tests;

public class ChatServiceTests
{
    [Fact]
    public async Task Answers_directly_when_no_tool_is_needed()
    {
        var model = new ScriptedChatClient(Text("Hi Alex! What are you training today?"));

        var response = await Service(model).AskAsync(Ask("hey"), userId: 1);

        Assert.Equal("Hi Alex! What are you training today?", response.Reply);
        Assert.Single(model.Requests);
        Assert.Equal(ChatRole.System, model.Requests[0].Messages[0].Role);
        Assert.Equal("hey", model.Requests[0].Messages[^1].Text);
    }

    [Fact]
    public async Task Sends_the_configured_temperature_token_limit_and_tools()
    {
        var model = new ScriptedChatClient(Text("Hi."));

        await Service(model, new EchoTool()).AskAsync(Ask("hey"), userId: 1);

        var options = model.Requests[0].Options!;
        Assert.Equal(0.5f, options.Temperature);
        Assert.Equal(700, options.MaxOutputTokens);
        var tool = Assert.IsAssignableFrom<AIFunctionDeclaration>(Assert.Single(options.Tools!));
        Assert.Equal("echo", tool.Name);
        Assert.Equal("object", tool.JsonSchema.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Runs_a_tool_and_sends_the_result_back_before_answering()
    {
        var model = new ScriptedChatClient(
            Calls(new FunctionCallContent("call_1", "echo", new Dictionary<string, object?> { ["text"] = "bench" })),
            Text("Done."));

        var response = await Service(model, new EchoTool()).AskAsync(Ask("find bench"), userId: 1);

        Assert.Equal("Done.", response.Reply);
        var second = model.Requests[1].Messages;
        Assert.Equal(ChatRole.Assistant, second[^2].Role);
        Assert.Equal("call_1", second[^2].Contents.OfType<FunctionCallContent>().Single().CallId);
        Assert.Equal(ChatRole.Tool, second[^1].Role);
        Assert.Equal("call_1", ToolResult(second[^1]).CallId);
        Assert.Contains("bench", ToolResult(second[^1]).Result?.ToString());
    }

    [Fact]
    public async Task Sends_tool_errors_back_to_the_model_as_json()
    {
        var model = new ScriptedChatClient(Calls(new FunctionCallContent("call_1", "fail", null)), Text("Sorry."));

        var response = await Service(model, new FailingTool()).AskAsync(Ask("do it"), userId: 1);

        Assert.Equal("Sorry.", response.Reply);
        var result = JsonDocument.Parse(ToolResult(model.Requests[1].Messages[^1]).Result!.ToString()!).RootElement;
        Assert.Equal("The \"name\" argument is required.", result.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Reports_an_unknown_tool_to_the_model_instead_of_failing()
    {
        var model = new ScriptedChatClient(Calls(new FunctionCallContent("call_1", "delete_everything", null)), Text("Sorry."));

        await Service(model, new EchoTool()).AskAsync(Ask("do it"), userId: 1);

        Assert.Contains("not found", ToolResult(model.Requests[1].Messages[^1]).Result?.ToString());
    }

    [Fact]
    public async Task Stops_after_five_tool_rounds_and_asks_for_a_text_answer()
    {
        var loop = Calls(new FunctionCallContent("call", "echo", null));
        var model = new ScriptedChatClient(loop, loop, loop, loop, loop, Text("Here's what I found."));

        var response = await Service(model, new EchoTool()).AskAsync(Ask("loop"), userId: 1);

        Assert.Equal("Here's what I found.", response.Reply);
        Assert.Equal(6, model.Requests.Count);
        Assert.All(model.Requests[..5], r => Assert.NotEmpty(r.Options!.Tools!));
        Assert.Empty(model.Requests[5].Options!.Tools ?? []);
    }

    [Fact]
    public async Task Refuses_politely_when_no_model_is_configured()
    {
        var service = new ChatService([], new FixedInstructions(), NullLogger<ChatService>.Instance, chatClient: null);

        var error = await Assert.ThrowsAsync<ChatUnavailableException>(() => service.AskAsync(Ask("hi"), userId: 1));

        Assert.Equal("The assistant isn't set up on this server.", error.Message);
    }

    [Theory]
    [InlineData(402, "The assistant has run out of credit for now.")]
    [InlineData(429, "The assistant is busy. Please try again in a minute.")]
    [InlineData(500, "The assistant couldn't answer right now. Please try again.")]
    public void Provider_errors_get_a_friendly_message(int status, string message)
    {
        Assert.Equal(message, ProviderErrorChatClient.MessageFor(status));
    }

    [Fact]
    public async Task Provider_failures_become_chat_unavailable()
    {
        var model = new ScriptedChatClient { Failure = new InvalidOperationException("bad response") };

        var error = await Assert.ThrowsAsync<ChatUnavailableException>(() => Service(model).AskAsync(Ask("hi"), userId: 1));

        Assert.Equal("The assistant couldn't answer right now. Please try again.", error.Message);
    }

    [Fact]
    public async Task Calculator_tool_asks_for_values_that_are_missing()
    {
        var tool = new OneRepMaxTool(new CalculatorService());
        var args = new ToolArguments(JsonDocument.Parse("""{"weight":100}""").RootElement);

        var error = await Assert.ThrowsAsync<ToolArgumentException>(() => tool.RunAsync(args, Context()));

        Assert.Contains("Enter the reps you performed.", error.Message);
    }

    [Fact]
    public async Task Calculator_tool_reads_the_models_arguments()
    {
        var tool = new OneRepMaxTool(new CalculatorService());
        var args = new ToolArguments(JsonDocument.Parse("""{"weight":"100","reps":5}""").RootElement);

        var result = (OneRepMaxResult)await tool.RunAsync(args, Context());

        Assert.Equal(117, result.OneRepMax);
    }

    /// <summary>The service over the same middleware the app uses, with a scripted model at the bottom.</summary>
    private static ChatService Service(ScriptedChatClient model, params IChatTool[] tools)
    {
        var settings = new OpenRouterOptions { ApiKey = "test", Model = "test", Temperature = 0.5, MaxTokens = 700 };
        var client = new ChatClientBuilder(model).AddMiddleware(settings).Build();
        return new ChatService(tools, new FixedInstructions(), NullLogger<ChatService>.Instance, client);
    }

    private static ChatRequest Ask(string message) => new() { Message = message };

    private static ToolContext Context() => new(1, new ChatCards());

    private static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    private static ChatResponse Calls(params FunctionCallContent[] calls) =>
        new(new ChatMessage(ChatRole.Assistant, [.. calls]));

    private static FunctionResultContent ToolResult(ChatMessage message) =>
        message.Contents.OfType<FunctionResultContent>().Single();

    private record Request(List<ChatMessage> Messages, ChatOptions? Options);

    /// <summary>Plays back prepared responses and records every request, so tests can check what the model was sent.</summary>
    private class ScriptedChatClient(params ChatResponse[] responses) : IChatClient
    {
        private readonly Queue<ChatResponse> _responses = new(responses);

        public List<Request> Requests { get; } = [];

        public Exception? Failure { get; init; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(new Request([.. messages], options?.Clone()));
            if (Failure is not null) throw Failure;

            // Fresh copies, because the function invocation middleware marks the calls it has handled.
            var next = _responses.Dequeue();
            return Task.FromResult(new ChatResponse([.. next.Messages.Select(Copy)]));
        }

        private static ChatMessage Copy(ChatMessage message) =>
            new(message.Role, [.. message.Contents.Select(content => content switch
            {
                FunctionCallContent call => new FunctionCallContent(call.CallId, call.Name, call.Arguments),
                TextContent text => new TextContent(text.Text),
                _ => content,
            })]);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private class EchoTool : IChatTool
    {
        public string Name => "echo";
        public string Description => "Repeats its arguments.";
        public JsonElement Parameters { get; } = ToolJson.Schema("""{ "type": "object" }""");

        public Task<object> RunAsync(ToolArguments args, ToolContext context) =>
            Task.FromResult<object>(new { Echo = args.Json.GetRawText() });
    }

    private class FailingTool : IChatTool
    {
        public string Name => "fail";
        public string Description => "Always needs a name.";
        public JsonElement Parameters { get; } = ToolJson.Schema("""{ "type": "object" }""");

        public Task<object> RunAsync(ToolArguments args, ToolContext context) =>
            Task.FromResult<object>(args.Required("name"));
    }

    private class FixedInstructions : IChatInstructions
    {
        public Task<string> BuildAsync(int userId, string? language) => Task.FromResult("You are a test assistant.");
    }
}
