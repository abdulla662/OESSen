using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OES.Helper.Enums;
using OES.Helper.General;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace OES.API.Extensions
{
    public static class AIServiceExtensions
    {
        public static IServiceCollection AddAIService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<AISettings>().Bind(configuration.GetSection(nameof(AISettings)));

            var settings = configuration.GetSection(nameof(AISettings)).Get<AISettings>();

            switch (settings.Provider)
            {
                case AIProvider.Ollama:
                    RegisterOllama(services, settings);
                    break;

                case AIProvider.OpenAI:
                    RegisterOpenAI(services, settings);
                    break;

                case AIProvider.AzureOpenAI:
                    RegisterAzureOpenAI(services, settings);
                    break;

                case AIProvider.OpenRouter:
                    RegisterOpenRouter(services, settings);
                    break;

                default:
                    throw new NotSupportedException($"The AI provider '{settings.Provider}' is not supported.");
            }

            return services;
        }

        private static void RegisterOpenAI(IServiceCollection services, AISettings settings)
        {
            var builder = services.AddChatClient(_ =>
            {
                var options = CreateOpenAIClientOptions(settings);

                var client = new ChatClient(settings.ModelName, new ApiKeyCredential(settings.ApiKey), options);

                return client.AsIChatClient();
            });

            ConfigureMiddleware(builder, settings);
        }

        private static void RegisterOpenRouter(IServiceCollection services, AISettings settings)
        {
            var builder = services.AddChatClient(_ =>
            {
                var options = CreateOpenAIClientOptions(settings);

                options.Endpoint = new Uri(settings.Endpoint);

                var client = new ChatClient(
                    model: settings.ModelName,
                    credential: new ApiKeyCredential(settings.ApiKey),
                    options: options);

                return client.AsIChatClient();
            });

            ConfigureMiddleware(builder, settings);
        }

        private static void RegisterAzureOpenAI(IServiceCollection services, AISettings settings)
        {
            var builder = services.AddChatClient(_ =>
            {
                var options = new AzureOpenAIClientOptions();

                ApplyTimeout(options, settings);

                var client = new AzureOpenAIClient(
                    new Uri(settings.Endpoint),
                    new AzureKeyCredential(settings.ApiKey), options);

                return client.GetChatClient(settings.DeploymentName!).AsIChatClient();
            });

            ConfigureMiddleware(builder, settings);
        }

        private static void RegisterOllama(IServiceCollection services, AISettings settings)
        {
            services.AddHttpClient(MiscConstants.OllamaClient, client =>
            {
                client.BaseAddress = new Uri(settings.Endpoint);

                if (settings.TimeoutSeconds > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
                }

                if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    client.DefaultRequestHeaders.Add(MiscConstants.XAPIKey, settings.ApiKey);
                }
            });

            var builder = services.AddChatClient(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();

                var httpClient = factory.CreateClient(MiscConstants.OllamaClient);

                var client = new OllamaApiClient(httpClient, settings.ModelName);

                return client;
            });

            ConfigureMiddleware(builder, settings);
        }

        private static OpenAIClientOptions CreateOpenAIClientOptions(AISettings settings)
        {
            var options = new OpenAIClientOptions();
            ApplyTimeout(options, settings);
            return options;
        }

        private static void ApplyTimeout(ClientPipelineOptions options, AISettings settings)
        {
            if (settings.TimeoutSeconds > 0)
            {
                options.NetworkTimeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            }
        }

        private static void ConfigureMiddleware(ChatClientBuilder builder, AISettings settings)
        {
            builder.UseOpenTelemetry();
            builder.UseLogging();
        }
    }
}
