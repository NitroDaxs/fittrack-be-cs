using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;

namespace FitTrack.Infrastructure.Ai;

/// <summary>Builds the assistant's <see cref="IChatClient"/>: OpenRouter through the OpenAI connector.</summary>
public static class OpenRouterChatClient
{
    /// <summary>Model calls that may ask for tools. One more call without tools follows if the model is still calling them.</summary>
    public const int MaxToolRounds = 5;

    /// <summary>The OpenAI-compatible client that talks to OpenRouter's API.</summary>
    public static IChatClient Create(OpenRouterOptions settings)
    {
        // A long-lived HttpClient whose connections are recycled, so DNS changes are still picked up.
        var http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });
        http.DefaultRequestHeaders.Add("HTTP-Referer", settings.AppUrl);
        http.DefaultRequestHeaders.Add("X-Title", settings.AppTitle);

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(settings.BaseUrl),
            Transport = new HttpClientPipelineTransport(http),
            NetworkTimeout = TimeSpan.FromSeconds(60),
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
            UserAgentApplicationId = "FitTrack",
        };

        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options)
            .GetChatClient(settings.Model)
            .AsIChatClient();
    }

    /// <summary>
    /// The middleware around the model client, outermost first: provider errors become
    /// <see cref="ChatUnavailableException"/>, the configured temperature and token limit
    /// are applied, and tool calls are run by the function invocation loop.
    /// </summary>
    public static ChatClientBuilder AddMiddleware(this ChatClientBuilder builder, OpenRouterOptions settings) =>
        builder
            .Use((inner, services) => new ProviderErrorChatClient(inner,
                services.GetService<ILogger<ProviderErrorChatClient>>() ?? NullLogger<ProviderErrorChatClient>.Instance))
            .ConfigureOptions(options =>
            {
                options.Temperature ??= (float)settings.Temperature;
                options.MaxOutputTokens ??= settings.MaxTokens;
            })
            .UseFunctionInvocation(configure: client =>
            {
                client.MaximumIterationsPerRequest = MaxToolRounds;
                // Our tools share per-request state (the cards), so run them one at a time.
                client.AllowConcurrentInvocation = false;
            });
}
