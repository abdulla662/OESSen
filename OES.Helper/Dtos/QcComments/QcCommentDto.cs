

namespace OES.Helper.Dtos.QcComments
{
    public class QcCommentDto
    {
        public long Id { get; set; }
        public string Comment { get; set; }
        public string QCGivenStatus { get; set; }
        public string QCGivenStatusDisplay => QCGivenStatus.ToLocalizedString<QuestionStatus>();
        public string QCAuthor { get; set; }
        public long QuestionMetadataId { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
