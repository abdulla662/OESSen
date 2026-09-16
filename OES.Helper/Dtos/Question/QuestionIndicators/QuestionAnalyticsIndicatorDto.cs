namespace OES.Helper.Dtos.Question.QuestionIndicators
{
    public sealed record QuestionAnalyticsIndicatorDto(
        long QuestionId,
        string QuestionCode,
        int TotalAnswered,
        int CorrectCount,
        int WrongCount,
        int Percentage
    )
    {
        public string PercentageDisplay => $"{Percentage}%";
    }
}
