namespace OES.Helper.Dtos.Question.QuestionVersions
{
    public class MatchingPairQuestionItemVersionDto
    {
        public long Id { get; set; }
        public long VersionNumber { get; set; }
        public string Body { get; set; }
        public int ColumnOrder { get; set; }
        public bool IsDataSource { get; set; }
    }
}
