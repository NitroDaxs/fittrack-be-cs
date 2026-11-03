using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FitTrack.Application.Chat;

/// <summary>
/// Exposes one of our <see cref="IChatTool"/>s to Microsoft.Extensions.AI, so the function invocation
/// middleware can call it. Any failure goes back to the model as {"error": "..."} instead of throwing.
/// </summary>
public class ChatToolFunction(IChatTool tool, ToolContext context, ILogger logger) : AIFunction
{
    public override string Name => tool.Name;

    public override string Description => tool.Description;

    public override JsonElement JsonSchema => tool.Parameters;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToElement<IDictionary<string, object?>>(arguments, ToolJson.Options);

        var result = await RunAsync(json);
        logger.LogInformation("Tool {Tool}({Arguments}) -> {Result}", Name, json.GetRawText(), Preview(result));
        return result;
    }

    private async Task<string> RunAsync(JsonElement arguments)
    {
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
            logger.LogError(e, "The {Tool} tool failed.", Name);
            return ToolJson.Error(
                "The tool failed. Try different arguments, or tell the person you couldn't look it up.");
        }
    }

    private static string Preview(string json) => json.Length <= 300 ? json : json[..300] + "...";
}
