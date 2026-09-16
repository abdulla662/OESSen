using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.GroupsTempleates;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.DuplicateGenericTempleateDialog
{
    public partial class DuplicateGenericTemplateDialog : ComponentBase
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Inject]
        public IBlazOesRoleTemplateService IBlazOesTemplateService { get; set; } = default!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = default!;

        [Parameter]
        public Guid TemplateId { get; set; }

        [Parameter]
        public string DialogTitle { get; set; } = string.Empty;

        [Parameter]
        public string DialogMessage { get; set; } = string.Empty;

        [Parameter]
        public string PlaceholderText { get; set; } = string.Empty;

        [Parameter]
        public string ConfirmButtonText { get; set; } = string.Empty;

        [Parameter]
        public string InitialValue { get; set; } = string.Empty;

        private string InputValue { get; set; } = string.Empty;

        private bool IsProcessing { get; set; }

        protected override void OnInitialized()
        {
            InputValue = UseOrDefault(InitialValue, string.Empty);
            DialogTitle = UseOrDefault(DialogTitle, Resource.DuplicateTemplate);
            DialogMessage = UseOrDefault(DialogMessage, Resource.EnterNewTemplateName);
            PlaceholderText = UseOrDefault(PlaceholderText, Resource.TypeNewName);
            ConfirmButtonText = UseOrDefault(ConfirmButtonText, Resource.Copy);
        }

        private static string UseOrDefault(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

        private async Task SubmitAsync()
        {
            if (string.IsNullOrWhiteSpace(InputValue))
            {
                return;
            }

            IsProcessing = true;

            var response = await IBlazOesTemplateService.DuplicateTemplateAsync(TemplateId, InputValue);

            IsProcessing = false;

            if (response?.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(
                    string.Format(Resource.TemplateDuplicatedSuccessfully, InputValue),
                    Severity.Success);

                MudDialog.Close(DialogResult.Ok(InputValue));
            }
            else
            {
                Snackbar.Add(response?.Message ?? Resource.Error, Severity.Error);
            }
        }

        private void Cancel() => MudDialog.Cancel();
    }
}