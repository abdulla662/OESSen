namespace OES.Helper.Dtos.AIQuestionGenerator.Request
{
    public sealed record AIQuestionGenerationTextRequestDto
    {
        public required string DocumentText { get; init; }

        public required AIQuestionGenerationConfigurationsDto Configuration { get; init; }
    }
}
