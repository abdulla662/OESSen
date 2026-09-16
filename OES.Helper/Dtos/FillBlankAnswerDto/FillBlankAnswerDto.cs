namespace OES.Helper.Dtos.FillBlankAnswerDto
{
    public sealed record FillBlankAnswerDto
    {
        public int OrderId { get; set; }

        public string CorrectAnswer { get; set; }
    }
}
