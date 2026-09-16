namespace OES.Helper.Dtos.Candidate.Requests
{
    public sealed record AllocateLookUpsSchedulePaperRequestDto(
        long[] LookUpIds,
        long VenueId,
        long SchedulePaperId
    );
}
