namespace OES.Helper.Dtos.SyncQuestions
{
    public class MatchingPairQuestionItemInternal
    {
        public long Id { get; set; }

        public string Body { get; set; }

        public int ColumnOrder { get; set; }

        public long QuestionDetailsId { get; set; }

        public bool IsDataSource { get; set; }
    }
}
