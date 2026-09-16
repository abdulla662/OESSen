using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OES.Helper.General;

namespace OES.Services.Services.HealthChecks
{
    public class AIChatHealthCheck : IHealthCheck
    {
        private readonly AISettings _settings;
        private readonly IChatClient _chatClient;

        public AIChatHealthCheck(IOptions<AISettings> options, IChatClient chatClient)
        {
            _settings = options.Value;
            _chatClient = chatClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _chatClient.GetResponseAsync("Reply with only OK.", cancellationToken: cancellationToken);

                return HealthCheckResult.Healthy($"{_settings.Provider} model '{_settings.ModelName}' is available.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Unable to reach {_settings.Provider} model '{_settings.ModelName}'.", ex);
            }
        }
    }
}