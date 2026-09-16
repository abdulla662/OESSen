namespace OES.Helper.General
{
    public sealed record AIAsset
    {
        public required Guid Id { get; init; }

        public required byte[] Data { get; init; }

        public required string ContentType { get; init; }
    }
}
