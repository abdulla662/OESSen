using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.Subject;

namespace OES.Helper.Dtos.Question
{
    public class AIQuestionTemplateCreationDto
    {
        public string Name { get; set; } = string.Empty;

        public RootItemBankDto? SelectedItemBankRoot { get; set; } = new();

        public SelectedItemBankNodeFromDialogDto SelectedItemBankNodeFromDialogDto { get; set; } = new();

        public SelectedIloNodeFromDialogDto SelectedIloNodeFromDialogDto { get; set; } = new();

        public RootIloDto? SelectedIloRoot { get; set; } = new();

        public SubjectDto SelectedSubject { get; set; } = new();

        public QuestionCategoryDto SelectedCategory { get; set; } = new();

        public LanguageDto SelectedLanguage { get; set; } = new();

        public ProfileDto SelectedDifficultyProfile { get; set; } = new();

        public List<AIQuestionTypeCountDto> QuestionTypeRequests { get; set; } = [];

        public bool IsFromAI { get; set; } = false;
    }
}
