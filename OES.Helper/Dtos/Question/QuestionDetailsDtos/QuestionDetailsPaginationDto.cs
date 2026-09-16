using OES.Helper.Enums;

namespace OES.Helper.Dtos.Question.QuestionDetailsDtos
{
    public class QuestionMetadataPaginationDto
    {
        public long Id { get; set; } // This property should be still in the first place like this, in order to get proper Id in the target paginated list of this type.
        public string Code { get; set; }
        public bool UseArabicNumbers { get; set; }
        public string Subject { get; set; }
        public string? MaximumAnswerTime { get; set; }
        public string ItemBank { get; set; }
        public string ItemBankCode { get; set; }
        public string Type { get; set; }
        public string TypeDisplay => Type?.ToLocalizedString<QuestionType>();
        public string Category { get; set; }
        public string Language { get; set; }
        public QuestionStatus Status { get; set; }
        public string StatusDisplay => Status.ToLocalizedString();
        public List<QuestionDataDto> QuestionData { get; set; }
        public List<QuestionMetadataPaginationDto> SubQuestions { get; set; } = [];
    }
}
