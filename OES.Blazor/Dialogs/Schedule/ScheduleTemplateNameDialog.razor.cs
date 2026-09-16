using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace OES.Blazor.Dialogs.Schedule
{
    public partial class ScheduleTemplateNameDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        private string _templateName = string.Empty;

        private bool _isSubmitButtonHit = false;

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void SaveTemplate()
        {
            _isSubmitButtonHit = true;

            if (!string.IsNullOrWhiteSpace(_templateName))
            {
                MudDialog.Close(DialogResult.Ok(_templateName));
            }
        }
    }
}
