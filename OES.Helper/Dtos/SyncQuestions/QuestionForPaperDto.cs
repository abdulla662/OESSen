using OES.Helper.Enums;

namespace OES.Helper.Dtos.SyncQuestions
{
    public class QuestionForPaperDto
    {
        public long PaperItemBankQuestionId { get; set; } // Note: Id here is NOT question metadata id, it's ManualPaperItemBankQuestionSection id

        public long QuestionId { get; set; }

        public long DifficultyLevelId { get; set; }

        public long ItemBankId { get; set; }

        public long? SectionId { get; set; }

        public long FormId { get; set; }

        public double? Score { get; set; }

        public PaperQuestionStatus PaperQuestionStatus { get; set; }
    }
}
