
namespace OES.Helper.Dtos.Paper.Responses
{
    public class AutoSelectedQuestionsResponseDto
    {
        public long Id { get; set; }
        public long QuestionTypeId { get; set; }
        public string QuestionTypeName { get; set; }
        public long DifficultyLevelId { get; set; }
        public string DifficultyLevelName { get; set; }
        public long ItemBankId { get; set; }
        public string ItemBankName { get; set; }
        public long SelectedCount { get; set; }
        public long SubQuestionCount { get; set; }
        public string SectionName { get; set; }
        public double? Mark { get; set; } = 0.0;
        public List<string> QuestionCodes { get; set; } = new();
    }
}
