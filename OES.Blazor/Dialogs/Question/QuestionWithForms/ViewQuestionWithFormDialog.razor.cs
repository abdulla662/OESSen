using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Form;
using OES.Helper.Dtos.Form;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.Question.QuestionWithForms
{
    public partial class ViewQuestionWithFormDialog
    {
        [Inject] private IBlazFormService BlazFormService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public long PaperId { get; set; }
        [Parameter] public long QuestionId { get; set; }

        private QuestionWithFormsDto QuestionWithForms { get; set; }
        private bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            await LoadQuestionWithForms();
        }

        private async Task LoadQuestionWithForms()
        {
            IsLoading = true;

            QuestionWithForms = await BlazFormService.GetQuestionWithFormsAsync(PaperId, QuestionId);

            if (QuestionWithForms == null || string.IsNullOrEmpty(QuestionWithForms.QuestionCode))
            {
                Snackbar.Add(Resource.FailedLoadQuestionData, Severity.Error);
            }

            IsLoading = false;
        }

        private void Cancel() => MudDialog.Close();
    }
}