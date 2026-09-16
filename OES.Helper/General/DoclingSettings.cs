namespace OES.Helper.General
{
    public sealed record DoclingSettings
    {
        public string BaseUrl { get; init; } = string.Empty;

        public string AsyncConvertEndpoint { get; init; } = "/v1/convert/file/async";

        public string StatusEndpoint { get; init; } = "/v1/status/poll";

        public string ResultEndpoint { get; init; } = "/v1/result";

        public int PollIntervalSeconds { get; init; } = 2;

        public int TimeoutSeconds { get; init; } = 1000;
    }
}
