namespace OES.Helper.Dtos.Question.CountsOfQuestionTypes
{
    public class ItemBankQuestionCountDto
    {
        public long ItemBankId { get; set; }

        public List<QuestionTypeCountDto> QuestionTypes { get; set; }
    }
}
