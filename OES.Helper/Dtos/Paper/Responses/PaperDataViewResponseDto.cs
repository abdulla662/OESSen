using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Helper.Dtos.Paper.Responses
{
    public class PaperDataViewResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public string Abbreviation { get; set; }
        public float Duration { get; set; }
        public long TotalMarks { get; set; }
        public LanguageDto Language { get; set; }
        public PaperType Type { get; set; }
        public QuestionSelectionType QuestionSelectionType { get; set; }
        public ProfileDto DifficultyProfile { get; set; }
    }
}
