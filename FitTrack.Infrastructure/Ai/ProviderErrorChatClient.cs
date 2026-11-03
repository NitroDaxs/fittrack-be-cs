using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FitTrack.Infrastructure.Ai;

/// <summary>Turns failures from the model provider into a <see cref="ChatUnavailableException"/> with a friendly message.</summary>
public class ProviderErrorChatClient(IChatClient inner, ILogger<ProviderErrorChatClient> logger)
    : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        catch (ClientResultException e) when (e.Status > 0)
        {
            logger.LogError(e, "OpenRouter returned {Status}.", e.Status);
            throw new ChatUnavailableException(MessageFor(e.Status));
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(e, "OpenRouter didn't answer in time.");
            throw new ChatUnavailableException("The assistant took too long to answer. Please try again.");
        }
        catch (Exception e) when (e is ClientResultException or HttpRequestException)
        {
            logger.LogError(e, "Couldn't reach OpenRouter.");
            throw new ChatUnavailableException("The assistant can't be reached right now. Please try again later.");
        }
        catch (Exception e) when (e is not OperationCanceledException and not ChatUnavailableException)
        {
            // For example a provider error inside a 200 response, which the SDK can't read as a completion.
            logger.LogError(e, "OpenRouter's answer couldn't be used.");
            throw new ChatUnavailableException("The assistant couldn't answer right now. Please try again.");
        }
    }

    public static string MessageFor(int status) => status switch
    {
        402 => "The assistant has run out of credit for now.",
        429 => "The assistant is busy. Please try again in a minute.",
        _ => "The assistant couldn't answer right now. Please try again.",
    };
}
