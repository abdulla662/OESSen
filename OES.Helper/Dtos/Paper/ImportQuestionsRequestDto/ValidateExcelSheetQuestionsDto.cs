namespace OES.Helper.Dtos.Paper.ImportQuestionsRequestDto
{
    public sealed record ValidateExcelSheetQuestionsDto(
        HashSet<long> AvailableLanguageIds,
        long DeducedDifficultyProfileId,
        List<QuestionWithSectionNameDto> QuestionWithSectionNameDtos
    );

    public sealed record QuestionWithSectionNameDto(
        long QuestionId,
        string SectionName,
        int SubQuestionsCount
    );
}

