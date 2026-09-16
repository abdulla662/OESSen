namespace OES.Helper.Dtos.Paper.Responses
{
    public class DifficultyLevelSummaryResponseDto
    {
        public long DifficultyLevelId { get; set; }

        public string DifficultyLevelName { get; set; }

        public long NumberOfQuestion { get; set; } // Related to difficulty level in item bank

        public List<QuestionTypeResponseDto> QuestionTypeResponseDtos { get; set; } = [];
    }
}
