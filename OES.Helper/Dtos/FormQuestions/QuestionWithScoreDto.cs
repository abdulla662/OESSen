using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.FormQuestions
{
    public class QuestionWithScoreDto
    {
        public long QuestionId { get; set; }

        public double? Score { get; set; }

        public string SectionName { get; set; }

        public int OrderId { get; set; }

        public bool IsTimer { get; set; }

        public string? SectionInstruction { get; set; }

        public double? SectionTime { get; set; }

        public PaperQuestionStatus PaperQuestionStatus { get; set; }

        public QuestionMetadataPaginationDto Metadata { get; set; }
    }
}
