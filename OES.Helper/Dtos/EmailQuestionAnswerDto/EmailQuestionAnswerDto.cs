namespace OES.Helper.Dtos.EmailQuestionAnswerDto
{
    public sealed record EmailQuestionAnswerDto
    {
        public List<string> ExpectedToEmails { get; set; } = [];

        public List<string> ExpectedCcEmails { get; set; } = [];

        public string ExpectedSubject { get; set; } = "";

        public List<string> RequiredBodyKeywords { get; set; } = [];
    }
}
