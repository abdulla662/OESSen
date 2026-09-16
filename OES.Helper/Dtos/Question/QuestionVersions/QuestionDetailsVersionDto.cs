namespace OES.Helper.Dtos.Question.QuestionVersions
{
    public class QuestionDetailsVersionDto
    {
        public long QuestionMetadataId { get; set; }
        public int VersionNumber { get; set; }
        public long QuestionTypeId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public long LanguageId { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public string ModelAnswer { get; set; } = string.Empty;
        public bool HasShuffled { get; set; }
        public long? MaxWords { get; set; }
        public int MaxRecordingTimeInSeconds { get; set; }
        public bool UseArabicNumbers { get; set; }
        public string? AttachmentFileName { get; set; }
        public DateTime CreationDate { get; set; }
        public string? CreatedBy { get; set; }
        public List<QuestionChoiceVersionDto> Choices { get; set; } = [];
        public List<QuestionDetailsVersionDto> SubQuestions { get; set; } = [];
        public SegmentQuestionPropertiesVersionDto SegmentQuestionPropertiesVersionDto { get; set; }
        public List<MatchingPairQuestionItemVersionDto> MatchingPairQuestionItems { get; set; } = [];
    }
}
