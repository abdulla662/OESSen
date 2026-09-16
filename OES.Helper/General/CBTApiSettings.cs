namespace OES.Helper.General
{
    public sealed record CBTApiSettings
    {
        public string BaseUrl { get; init; }

        public string Endpoint { get; init; }

        public string PrivateKey { get; init; }

        public int TimeoutSeconds { get; init; }

        public int RetryAttempts { get; init; }

        public string SyncScheduleTime { get; init; } = "10:00";

        public bool AutoSyncEnabled { get; init; } = true;
    }
}
