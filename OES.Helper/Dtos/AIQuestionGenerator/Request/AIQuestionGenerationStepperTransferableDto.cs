using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.Subject;

namespace OES.Helper.Dtos.AIQuestionGenerator.Request
{
    public class AIQuestionGenerationStepperTransferableDto
    {
        public IBrowserFile? File { get; set; }

        public SelectedItemBankNodeFromDialogDto? SelectedItemBank { get; set; }

        public SelectedIloNodeFromDialogDto? SelectedIlo { get; set; }

        public SubjectDto? SelectedSubject { get; set; }

        public QuestionCategoryDto? SelectedCategory { get; set; }

        public LanguageDto? SelectedLanguage { get; set; }

        public ProfileDto? SelectedDifficultyProfile { get; set; }

        public List<AIQuestionTypeCountDto> QuestionTypeRequests { get; set; } = [];
    }

    public sealed record AIQuestionTypeCountDto
    {
        public long QuestionTypeId { get; init; }

        public string QuestionTypeName { get; init; } = string.Empty;

        public int Count { get; init; }
    }
}
