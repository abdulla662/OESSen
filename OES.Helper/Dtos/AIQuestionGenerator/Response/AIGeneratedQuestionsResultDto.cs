using OES.Helper.Dtos.OESUserGroups;

namespace OES.Helper.Dtos.AIQuestionGenerator.Response
{
    /// <summary>
    /// Wrapper containing a list of AI-generated questions.
    /// </summary>
    public sealed class AIGeneratedQuestionsResultDto
    {
        public List<AIQuestionMetadataDto> Questions { get; set; } = [];
    }

    /// <summary>
    /// Metadata for an AI-generated question, containing classification, configuration data, and a list of details.
    /// </summary>
    public sealed class AIQuestionMetadataDto
    {
        public string Code { get; set; } = string.Empty;

        public long QuestionTypeId { get; set; }

        public string QuestionTypeName { get; set; } = string.Empty;

        public long LayoutId { get; set; }

        public string LayoutName { get; set; } = string.Empty;

        public long DifficultyLevelId { get; set; }

        public string DifficultyLevelName { get; set; } = string.Empty;

        public long DifficultyProfileId { get; set; }

        public long SubjectId { get; set; }

        public string SubjectName { get; set; } = string.Empty;

        public long CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public long? IloId { get; set; }

        public string? IloName { get; set; }

        public long ItemBankId { get; set; }

        public string ItemBankName { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public decimal Delta { get; set; } = 0;

        public bool IsRoot { get; set; } = true;

        public long QuestionsExhaustionCount { get; set; }

        public bool ScientificEditorPanelEnabled { get; set; }

        public bool FileManagerEditorPanelEnabled { get; set; }

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];

        public List<AIQuestionDetailsDto> Details { get; set; } = [];
    }

    /// <summary>
    /// Details for an AI-generated question, containing the question body, language, and a list of choices.
    /// </summary>
    public sealed class AIQuestionDetailsDto
    {
        public string Body { get; set; } = string.Empty;

        public long LanguageId { get; set; }

        public string LanguageName { get; set; } = string.Empty;

        public string? Instructions { get; set; }

        public string? ModelAnswer { get; set; }

        public long? MaxWords { get; set; }

        public bool UseArabicNumbers { get; set; }

        public List<AIQuestionChoiceDto> Choices { get; set; } = [];
    }

    /// <summary>
    /// Choice data for multiple choice or true/false questions.
    /// </summary>
    public sealed class AIQuestionChoiceDto
    {
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public int Order { get; set; }
    }
}
