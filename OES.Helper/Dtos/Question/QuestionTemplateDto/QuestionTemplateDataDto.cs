namespace OES.Helper.Dtos.Question.QuestionTemplateDto
{
    public class QuestionTemplateDataDto
    {
        public long Id { get; set; }
        public string? Code { get; set; }
        public long? DifficultyProfileId { get; set; }
        public long? DifficultyLevelId { get; set; }
        public double? Delta { get; set; }
        public bool? IsRoot { get; set; }
        public int? MaximumAnswerTime { get; set; }
        public string? Author { get; set; }
        public long? QuestionTypeId { get; set; }
        public long? QuestionSubjectId { get; set; }
        public long? QuestionCategoryId { get; set; }
        public long? IloId { get; set; }
        public long? ItemBankId { get; set; }
        public long? QuestionLayoutId { get; set; }
        public string? IloName { get; set; }
        public string? ItemBankName { get; set; }

    }
}
