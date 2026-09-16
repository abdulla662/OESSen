namespace OES.Helper.Dtos.UploadFiles
{
    public class ExportQuestionDto
    {
        public List<long> QuestionIds { get; set; }
        public long TypeId { get; set; }
        public long LanguageId { get; set; }
    }
}
