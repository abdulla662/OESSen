using Microsoft.Extensions.AI;

namespace OES.API.BackgroundServices
{
    public class AIWarmupHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AIWarmupHostedService> _logger;

        public AIWarmupHostedService(IServiceProvider serviceProvider, ILogger<AIWarmupHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = WarmupAsync(cancellationToken);
            return Task.CompletedTask;
        }

        private async Task WarmupAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var chatClient = scope.ServiceProvider.GetRequiredService<IChatClient>();

                _logger.LogInformation("Warming up AI model...");

                var messages = new[]
                {
                    new ChatMessage(ChatRole.User, "ping")
                };

                var options = new ChatOptions
                {
                    MaxOutputTokens = 5
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();

                await chatClient.GetResponseAsync(messages, options, cancellationToken);

                sw.Stop();
                _logger.LogInformation("AI model warmup completed in {Elapsed}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI model warmup failed. First real request may be slow.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
