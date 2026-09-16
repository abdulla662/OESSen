
namespace OES.Helper.Dtos.QuestionLayout
{
    public sealed record MatchingPairRowView
    {
        public string Term { get; set; }

        public string SelectedAnswer { get; set; }

        public string CorrectAnswer { get; set; }

        public long LanguageId { get; set; }
    }
}
