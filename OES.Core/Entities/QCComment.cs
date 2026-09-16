namespace OES.Core.Entities
{
    public class QCComment : BaseEntity<long>
    {
        public long QuestionMetadataId { get; set; }

        public string Comment { get; set; }

        public string QCGivenStatus { get; set; }

        public string QCAuthor { get; set; }


        // Navigational Properties

        public QuestionMetadata QuestionMetadata { get; set; }
    }
}
