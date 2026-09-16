namespace OES.Helper.Dtos.Paper.Responses
{
    public class GetAutoQuestionMarkInfoResponseDto
    {
        public long AutoPaperItemBankQuestionSectionId { get; set; }
        public string Code { get; set; }
        public string ItemBankName { get; set; }
        public string DifficultyLevelName { get; set; }
        public string QuestionTypeName { get; set; }
        public long SubQuestionsCount { get; set; }
        public double? Mark { get; set; }
        public decimal? DeltaValue { get; set; } = 0.0m;
    }
}
