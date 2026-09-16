using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;

namespace OES.Helper.Dtos.Question
{
    public class QuestionDetailsForQCDto
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public string Instruction { get; set; }
        public string ModelAnswer { get; set; }
        public string LanguageName { get; set; }
        public long LanguageId { get; set; }
        public long QuestionMetadataId { get; set; }
        public List<ChoiceDataDto> QuestionsChoices { get; set; }
        public bool HasShuffled { get; set; }
        public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentFileName);
        public string? AttachmentFileName { get; set; }
        public long? MaxWords { get; set; }
        public int MaxRecordingTimeInSeconds { get; set; }
        public SegmentQuestionConfigDto? SegmentQuestionConfig { get; set; }
    }
}
