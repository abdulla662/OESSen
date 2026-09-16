namespace OES.Helper.Dtos.Schedule.Responses
{
    public sealed record VenueBatchDetailsDto(
        long BatchId,
        DateTime UploadDate,
        int CandidatesCount
    );
}
