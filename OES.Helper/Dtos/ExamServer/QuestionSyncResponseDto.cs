namespace OES.Helper.Dtos.ExamServer
{
    public class QuestionSyncResponseDto
    {
        public long OriginalQuestionId { get; set; }
        public long? ParentId { get; set; }
        public bool IsRoot { get; set; }
        public long PaperFormId { get; set; }
        public string? Code { get; set; }
        public string Type { get; set; }
        public bool IsAutoCorrectable { get; set; }
        public string? Layout { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string? Instructions { get; set; }
        public string? AttachmentFileName { get; set; }
        public string[]? ModelAnswerTexts { get; set; }
        public long[]? ModelAnswerIds { get; set; }
        public string Language { get; set; }
        public bool ChoicesShuffled { get; set; }
        public bool ScientificEditorPanelEnabled { get; set; }
        public bool FileManagerEditorPanelEnabled { get; set; }
        public bool UseArabicNumbers { get; set; }
        public double Score { get; set; }
        public decimal Delta { get; set; }
        public long? MaxWords { get; set; }
        public long? BlockId { get; set; }
        public long SectionId { get; set; }
        public List<Guid> FileIds { get; set; }
        public string PaperQuestionStatus { get; set; }
        public int MaxRecordingTimeInSeconds { get; set; }
        public List<QuestionSyncResponseDto> SubQuestions { get; set; }

        // JSON Properties

        public string? FileUploadSettings { get; set; }
        public string Choices { get; set; }
        public string MatchingPairQuestionItems { get; set; }
        public string? SegmentQuestionProperties { get;  set; }
    }
}