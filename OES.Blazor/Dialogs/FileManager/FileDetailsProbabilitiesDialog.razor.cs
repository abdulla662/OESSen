using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.FileDetails;

namespace OES.Blazor.Dialogs.FileManager
{
    public partial class FileDetailsProbabilitiesDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; }

        [Parameter] public UploadedFileAndSimilarities FileProbabilities { get; set; }


        private void ProceedDialog()
        {
            MudDialog.Close(true);
        }

        private void CloseDialog()
        {
            MudDialog.Close(false);
        }
    }
}
