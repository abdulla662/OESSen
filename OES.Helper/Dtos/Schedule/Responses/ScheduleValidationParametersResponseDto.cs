namespace OES.Helper.Dtos.Schedule.Responses
{
    public sealed record ScheduleValidationParametersResponseDto(
        long SchedulePapersCount,
        bool AnyPaperOnScheduleHasCandidates
    );
}
