namespace OES.Helper.General
{
    public sealed record MinioSettings
    {
        public string Endpoint { get; init; } = null!;
        public string AccessKey { get; init; } = null!;
        public string SecretKey { get; init; } = null!;
        public string BucketName { get; init; } = null!;
        public bool UseSSL { get; init; }
        public MinioAutoPurgeSettings AutoPurge { get; init; } = new();
    }

    public sealed record MinioAutoPurgeSettings
    {
        public bool Enabled { get; init; } = true;
        public int ExpireAfterDays { get; init; } = 7;
        public string Prefix { get; init; } = "payload_";
    }
}