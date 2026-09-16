namespace OES.Core.Entities
{
    public class QuestionsChoicesVersions : BaseEntity<long>
    {
        public long VersionNumber { get; set; }

        public string ChoiceText { get; set; }

        public bool IsCorrectAnswer { get; set; }

        public string? AttachmentFileName { get; set; }

        public long QuestionDetailsId { get; set; }
    }
}
