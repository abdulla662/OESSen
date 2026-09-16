using OES.Helper.Enums;

namespace OES.Helper.General
{
    public sealed record AISettings
    {
        public AIProvider Provider { get; init; } = AIProvider.Ollama;

        public string ModelName { get; init; } = string.Empty;

        public string ApiKey { get; init; } = string.Empty;

        public string Endpoint { get; init; } = string.Empty;

        public string? DeploymentName { get; init; } = null;

        public int TimeoutSeconds { get; init; } = 100;

        public float Temperature { get; init; } = 0.2f;

        public int MinOutputTokens { get; init; } = 1000;

        public int ContextLimit { get; init; } = 32768;

        public int SafetyMargin { get; init; } = 500;
    }
}
