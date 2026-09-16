namespace OES.Helper.Dtos.MarkingScheme
{
    public sealed record GenericMarkingSchemeApplicationDto<T>(
        double TotalMarkPerForm,
        T Data
    );

    public sealed record EqualDistributionForAutoDto(
        long AutoPaperItemBankQuestionSectionId,
        double? Mark
    );

    public sealed record EqualDistributionForManualDto(
        long QuestionMetadataId,
        double? Mark,
        long? FormId
    );

    public sealed record DifficultyLevelDistributionDto(
        long DifficultyLevelId,
        string DifficultyLevelName,
        long SelectedCount,
        double MarkPerQuestion,
        long? FormId // May be nullable or zero in case of 'Auto' paper
    );

    public sealed record ItemBankDistributionDto(
        long ItemBankId,
        string ItemBankName,
        double ItemBankMark,
        long QuestionCount,
        double QuestionMark,
        long? FormId // May be nullable or zero in case of ;Auto' paper
    );

    public sealed record WeightedExamDistributionDto(
        long QuestionMetadataId,
        decimal? DeltaValue,
        long? FormId // May be nullable or zero in case of 'Auto' paper
    );
}
