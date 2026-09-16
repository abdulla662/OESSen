namespace OES.Helper.Dtos.QueueSuspend
{
    public abstract record SuspensionContextBase
    {
        public required List<VenueInfo> AllVenues { get; init; }
        public required HashSet<string> NewSuspendedVenueCodes { get; init; }
        public required HashSet<string> ExistingSuspendedCodeSet { get; init; }
        public required HashSet<long> SyncingVenueIdSet { get; init; }
        public required bool IsInitiallyActive { get; init; }
    }
}
