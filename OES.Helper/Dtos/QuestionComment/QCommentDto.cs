namespace OES.Helper.Dtos.QuestionComment
{
    public class QCommentDto
    {
        public long QuestionMetaDataId { get; set; }

        public string Comment { get; set; }

        public QuestionStatus QCGivenStatus { get; set; }

        public string QCAuthor { get; set; }
    }
}
