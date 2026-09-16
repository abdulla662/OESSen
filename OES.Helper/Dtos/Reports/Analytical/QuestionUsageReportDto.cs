namespace OES.Helper.Dtos.Reports.Analytical
{
    public sealed record QuestionUsageReportDto(
        string Schedule,
        string Venue,
        string Paper,
        List<QuestionUsageItemDto> Items);

    public sealed record QuestionUsageItemDto(
        string QuestionCode,
        long SubQuestionId,
        string Topic,
        int NoOfStudents,
        int AnsweredCorrect,
        int AnsweredIncorrect,
        int NotAttempted,
        double CorrectResponsePct,
        double IncorrectResponsePct,
        string DifficultyLevel);
}
