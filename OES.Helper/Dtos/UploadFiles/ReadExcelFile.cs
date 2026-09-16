namespace OES.Helper.Dtos.UploadFiles
{
    public class ReadExcelFile
    {
        public string QuestionCode { get; set; }

        public string QuestionBody { get; set; }

        public List<UploadChoicesDto> Choices { get; set; }

        public string ModelAnswer { get; set; }

        public string Type { get; set; }

        public string ItemBankCode { get; set; }
    }
}
