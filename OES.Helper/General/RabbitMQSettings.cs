namespace OES.Helper.General
{
    public sealed record RabbitMQSettings
    {
        public string Host { get; init; } = null!;
        public ushort Port { get; init; }
        public string Username { get; init; } = null!;
        public string Password { get; init; } = null!;
        public string VirtualHost { get; init; } = null!;
    }
}