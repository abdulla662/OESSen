
namespace OES.Helper.General
{
    public sealed record VenueDatabase
    {
        public const string SectionName = "VenueDatabase";

        public string Username { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;

        public int Port { get; init; } = 3308;

        public int ConnectionTimeoutSeconds { get; init; } = 300;

        public string MalazFallbackVenueCode { get; init; } = "Malaz2";

        public string MalazFallbackDatabase { get; init; } = "malaz_localcesdb";
    }
}
