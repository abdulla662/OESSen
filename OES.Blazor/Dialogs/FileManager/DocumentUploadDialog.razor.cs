using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DocumentService;
using OES.Helper.ResourceFiles;
using SharedHelper.Services;

namespace OES.Blazor.Dialogs.FileManager
{
    public partial class DocumentUploadDialog
    {
        [Inject] private IBlazDocumentService BlazDocumentService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public Guid CurrentFolderId { get; set; }


        private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
        private string _dragClass = DefaultDragClass;
        private bool _processing;
        private MudFileUpload<IBrowserFile> _mudFileUploadComponent;
        private IBrowserFile _fileToUpload;

        private async Task ClearAsync()
        {
            await _mudFileUploadComponent.ClearAsync();

            _fileToUpload = null;

            ClearDragClass();
        }

        private void OnInputFileChanged(InputFileChangeEventArgs e)
        {
            ClearDragClass();

            _ = ValidateAndSetFileAsync(e.File);
        }

        private async Task ValidateAndSetFileAsync(IBrowserFile file)
        {
            var (isValid, error) = await DocLibFileUploadValidationService.ValidateAsync(file).ConfigureAwait(false);
            if (!isValid)
            {
                _fileToUpload = null;
                await InvokeAsync(() => Snackbar.Add(error ?? Resource.FileTypeNotAllowed, Severity.Error));
                return;
            }

            _fileToUpload = file;
            await InvokeAsync(StateHasChanged);
        }

        private async Task UploadFileAsync()
        {
            _processing = true;

            var response = await BlazDocumentService.UploadDocumentAsync(_fileToUpload, CurrentFolderId);

            if (response.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);

                MudDialog.Close(DialogResult.Ok(response.Data));
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _processing = false;
        }

        private void SetDragClass() => _dragClass = $"{DefaultDragClass} mud-border-primary";

        private void ClearDragClass() => _dragClass = DefaultDragClass;

        private void Close() => MudDialog.Cancel();
    }
}
