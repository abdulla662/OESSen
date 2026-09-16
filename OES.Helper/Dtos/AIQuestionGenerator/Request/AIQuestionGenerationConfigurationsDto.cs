namespace OES.Helper.Dtos.AIQuestionGenerator.Request
{
    public sealed record AIQuestionGenerationConfigurationsDto
    {
        public required IReadOnlyCollection<AIQuestionTypeCountDto> QuestionTypes { get; init; }

        public bool RequireAnswersFromDocumentOnly { get; init; } = true;

        public bool GeneratePlausibleDistractors { get; init; } = true;

        public required EntityReferenceDto ItemBank { get; init; }

        public required EntityReferenceDto Language { get; init; }

        public required EntityReferenceDto Subject { get; init; }

        public required EntityReferenceDto Category { get; init; }

        public required EntityReferenceDto DifficultyProfile { get; init; }

        public EntityReferenceDto? Ilo { get; init; }

        public required string Author { get; init; }

        public long QuestionsExhaustionCount { get; init; }

        public bool ScientificEditorPanelEnabled { get; init; }

        public bool FileManagerEditorPanelEnabled { get; init; }
    }

    public sealed record EntityReferenceDto(long Id, string Name);
}
