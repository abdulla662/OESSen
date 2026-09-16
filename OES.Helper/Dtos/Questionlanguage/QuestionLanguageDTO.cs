
namespace OES.Helper.Dtos.Questionlanguage
{
    public class QuestionLanguageDTO
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public int NumberOfChoices { get; set; }
        public string Code { get; set; }
        public long LanguageId { get; set; }
        public string LanguageName { get; set; }
        public bool HasChild { get; set; } = false;
        public bool UseArabicNumbers { get; set; }
    }
}
