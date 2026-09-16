using OES.Helper.Enums;

namespace OES.Helper.Dtos.Form
{
    public class EditFormDto
    {
        public long FormId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public List<FormQuestionEditDto> Questions { get; set; } = [];
    }

    public class FormQuestionEditDto
    {
        public long QuestionId { get; set; }
        public double Score { get; set; }
        public PaperQuestionStatus FormQuestionStatus { get; set; }
    }
}
