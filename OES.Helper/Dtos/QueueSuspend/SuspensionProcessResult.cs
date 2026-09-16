namespace OES.Helper.Dtos.QueueSuspend
{
    public record SuspensionProcessResult(
        HashSet<long> SuspendedVenueIds,
        HashSet<long> UnsuspendedVenueIds,
        List<string> FailedVenueNames
    );
}
