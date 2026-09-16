using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Dialogs.Question.QuestionLanguageSelectionDialog
{
    public partial class QuestionLanguageSelectionDialog
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }

        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }


        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public long RootComprehensionQuestionMetadataId { get; set; }

        [Parameter] public long ComprehensionSubQuestionMetadataId { get; set; }

        [Parameter] public string ConfirmationButtonText { get; set; }

        private LanguageDto SelectedLanguage { get; set; } = new();

        private List<LanguageDto> LanguagesList { get; set; } = [];


        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;


        protected override async Task OnInitializedAsync()
        {
            LanguagesList = await BlazQuestionLanguage.GetAllQuestionDetailsLanguagesGroupAsync(RootComprehensionQuestionMetadataId);
        }

        private bool ValidateForm()
        {
            return SelectedLanguage.Id > 0;
        }

        private async Task OnSubmitAsync()
        {
            _isSubmitButtonHit = true;

            _processing = true;

            var isFormValid = ValidateForm();

            if (!isFormValid)
            {
                Snackbar.Add(@Resource.CannotProceedWithLanguageUnselected, Severity.Error);
                _processing = false;
                return;
            }

            var response = await BlazQuestionService.GetQuestionByMetaDataIdAndLanguageId(ComprehensionSubQuestionMetadataId, SelectedLanguage.Id);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Error);
                _processing = false;
                return;
            }

            MudDialog.Close(DialogResult.Ok(response.Data));

            _processing = false;
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
