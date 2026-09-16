using OES.Helper.Enums;

namespace OES.Helper.Dtos.UploadFiles
{
    public class UploadQuestionDetailsDto
    {
        public long Id { get; set; }
        public string? QuestionCode { get; set; }
        public string? Body { get; set; }
        public string? ModelAnswer { get; set; }
        public List<UploadChoicesDto>? Choices { get; set; }
        public long QuestionTypeId { get; set; }
        public string QuestionType { get; set; }
        public bool HasWarning { get; set; }
        public string Warning { get; set; }
        public string WarningInfo { get; set; }
        public long? ItemBankId { get; set; }
        public string ItemBankCode { get; set; }
        public int DisplayOrder { get; set; }
        public UploadMode UploadMode { get; set; }
        public int MaxRecordingTimeInSeconds { get; set; }
    }
}
