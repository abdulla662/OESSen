using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.Document.Response;

namespace OES.Helper.Dtos.AIQuestionGenerator.Request
{
    public sealed record AIQuestionGenerationPromptContextDto
    {
        public required string DocumentText { get; init; }

        public required EntityReferenceDto Language { get; init; }

        public required EntityReferenceDto Category { get; init; }

        public required EntityReferenceDto Subject { get; init; }

        public required EntityReferenceDto ItemBank { get; init; }

        public EntityReferenceDto? Ilo { get; init; }

        public required EntityReferenceDto DifficultyProfile { get; init; }

        public bool RequireAnswersFromDocumentOnly { get; init; } = true;

        public bool GeneratePlausibleDistractors { get; init; } = true;

        public required string Author { get; init; }

        public long QuestionsExhaustionCount { get; init; }

        public bool ScientificEditorPanelEnabled { get; init; }

        public bool FileManagerEditorPanelEnabled { get; init; }

        public List<DifficultyLevelDto>? DifficultyLevels { get; init; }

        public bool HasImages { get; init; }

        public List<ExtractionResult.ExtractedImage>? ImageCandidates { get; init; }

        public required IReadOnlyCollection<AIQuestionTypeCountDto> QuestionTypes { get; init; }
    }
}
