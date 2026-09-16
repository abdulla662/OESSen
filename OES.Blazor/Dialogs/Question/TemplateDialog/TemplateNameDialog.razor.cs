using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.Question.TemplateDialog
{
    public partial class TemplateNameDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        private bool _isSaveClicked = false;
        private bool IsTemplateNameInvalid => _isSaveClicked && string.IsNullOrEmpty(TemplateName);
        private string TemplateName { get; set; }
        private string TemplateNameErrorText => IsTemplateNameInvalid ? Resource.TemplateNameIsRequired : string.Empty;

        private void SaveTemplateName()
        {
            _isSaveClicked = true;

            if (IsTemplateNameInvalid)
                return;

            MudDialog.Close(DialogResult.Ok(TemplateName));
        }

        private void Cancel()
        {
            MudDialog.Close();
        }
    }
}
