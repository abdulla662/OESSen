namespace OES.Helper.Dtos.UploadFiles
{
    public class ExportQuestionInItemBankDto
    {
        public List<long> ItemBanksIds { get; set; }

        public long LanguageId { get; set; }

        public bool WithTags { get; set; }
    }
}
