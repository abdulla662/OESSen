using OES.Helper.Dtos.Question.SegmentQuestionDtos;

namespace OES.Helper.Dtos.SyncQuestions
{
    public class QuestionDetailsInternal
    {
        public long QuestionId { get; set; }
        public string Body { get; set; }
        public string? Instructions { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? ModelAnswer { get; set; }
        public bool? HasShuffled { get; set; }
        public string LanguageName { get; set; }
        public long LanguageId { get; set; }
        public int MaxRecordingTimeInSeconds { get; set; }
        public bool UseArabicNumbers { get; set; }
        public long? MaxWords { get; set; }
        public SyncSegmentPropertiesDto? SegmentQuestionProperties { get; set; }
        public List<ChoiceInternal> Choices { get; set; }
        public List<MatchingPairQuestionItemInternal> MatchingPairQuestionItems { get; set; }
    }
}
