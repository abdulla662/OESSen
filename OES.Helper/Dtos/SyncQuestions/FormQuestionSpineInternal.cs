using OES.Helper.Enums;

namespace OES.Helper.Dtos.SyncQuestions
{
    public class FormQuestionSpineInternal
    {
        public long FormId { get; set; }
        public long QuestionId { get; set; }
        public long QuestionTypeId { get; set; }
        public PaperQuestionStatus FormQuestionStatus { get; set; }
        public long DifficultyLevelId { get; set; }
        public double? Score { get; set; }
        public long PaperId { get; set; }
        public long PaperLanguageId { get; set; }
        public QuestionSelectionType PaperSubType { get; set; }
        public long? BlockId { get; set; }
    }
}
