namespace OES.Helper.Dtos.ExamServer
{
    public sealed record StageCategoryDecisionPathDto(
        long Id,
        long QuestionCategoryId,
        string QuestionCategoryName,
        decimal? DecisionPathValue
    );
}
