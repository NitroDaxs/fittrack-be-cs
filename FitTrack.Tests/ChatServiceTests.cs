using System.Text.Json;
using FitTrack.Application.Abstractions.Ai;
using FitTrack.Application.Chat;
using FitTrack.Application.Chat.Tools;
using FitTrack.Application.Common;
using FitTrack.Application.Dtos;
using FitTrack.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitTrack.Tests;

public class ChatServiceTests
{
    [Fact]
    public async Task Answers_directly_when_no_tool_is_needed()
    {
        var model = new ScriptedModel(Text("Hi Alex! What are you training today?"));

        var response = await Service(model).AskAsync(Ask("hey"), userId: 1);

        Assert.Equal("Hi Alex! What are you training today?", response.Reply);
        Assert.Single(model.Requests);
        Assert.Equal(ChatRole.System, model.Requests[0][0].Role);
        Assert.Equal("hey", model.Requests[0][^1].Content);
    }

    [Fact]
    public async Task Runs_a_tool_and_sends_the_result_back_before_answering()
    {
        var model = new ScriptedModel(
            Calls(new ToolCall("call_1", "echo", """{"text":"bench"}""")),
            Text("Done."));

        var response = await Service(model, new EchoTool()).AskAsync(Ask("find bench"), userId: 1);

        Assert.Equal("Done.", response.Reply);
        var second = model.Requests[1];
        Assert.Equal(ChatRole.Assistant, second[^2].Role);
        Assert.Equal("call_1", second[^2].ToolCalls![0].Id);
        Assert.Equal(ChatRole.Tool, second[^1].Role);
        Assert.Equal("call_1", second[^1].ToolCallId);
        Assert.Contains("bench", second[^1].Content);
    }

    [Fact]
    public async Task Reports_an_unknown_tool_to_the_model_instead_of_failing()
    {
        var model = new ScriptedModel(Calls(new ToolCall("call_1", "delete_everything", "{}")), Text("Sorry."));

        await Service(model).AskAsync(Ask("do it"), userId: 1);

        Assert.Contains("no tool called", model.Requests[1][^1].Content);
    }

    [Fact]
    public async Task Stops_after_five_tool_rounds_and_asks_for_a_text_answer()
    {
        var loop = Calls(new ToolCall("call", "echo", "{}"));
        var model = new ScriptedModel(loop, loop, loop, loop, loop, Text("Here's what I found."));

        var response = await Service(model, new EchoTool()).AskAsync(Ask("loop"), userId: 1);

        Assert.Equal("Here's what I found.", response.Reply);
        Assert.Equal(6, model.Requests.Count);
        Assert.False(model.ToolCallsAllowed[^1]);
    }

    [Fact]
    public async Task Refuses_politely_when_no_model_is_configured()
    {
        var model = new ScriptedModel { IsConfigured = false };

        await Assert.ThrowsAsync<ChatUnavailableException>(() => Service(model).AskAsync(Ask("hi"), userId: 1));
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

    private static ChatService Service(IChatModel model, params IChatTool[] tools) =>
        new(model, tools, new FixedInstructions(), NullLogger<ChatService>.Instance);

    private static ChatRequest Ask(string message) => new() { Message = message };

    private static ToolContext Context() => new(1, new ChatCards());

    private static ChatModelReply Text(string text) => new(text, []);

    private static ChatModelReply Calls(params ToolCall[] calls) => new(null, calls);

    /// <summary>Plays back prepared replies and records every request, so tests can check what the model was sent.</summary>
    private class ScriptedModel(params ChatModelReply[] replies) : IChatModel
    {
        private readonly Queue<ChatModelReply> _replies = new(replies);

        public bool IsConfigured { get; init; } = true;
        public List<List<ChatMessage>> Requests { get; } = [];
        public List<bool> ToolCallsAllowed { get; } = [];

        public Task<ChatModelReply> CompleteAsync(
            IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools, bool allowToolCalls, CancellationToken cancellationToken)
        {
            Requests.Add([.. messages]);
            ToolCallsAllowed.Add(allowToolCalls);
            return Task.FromResult(_replies.Dequeue());
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

    private class FixedInstructions : IChatInstructions
    {
        public Task<string> BuildAsync(int userId, string? language) => Task.FromResult("You are a test assistant.");
    }
}