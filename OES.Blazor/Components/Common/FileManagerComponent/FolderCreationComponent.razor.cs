using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.FolderService;
using OES.Helper.Dtos.Folder.Request;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.ResourceFiles;
using System.Text.RegularExpressions;

namespace OES.Blazor.Components.Common.FileManagerComponent
{
    public partial class FolderCreationComponent : ComponentBase
    {
        [Inject] private IBlazFolderService BlazFolderService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public Guid ParentFolderId { get; set; }
        [Parameter] public EventCallback<FolderCreationResponseDto> OnCreated { get; set; }
        [Parameter] public EventCallback OnCancelled { get; set; }

        private bool _processing = false;

        private string _folderName = string.Empty;


        private async Task CreateFolderAsync()
        {
            var validationResult = ValidateTextInput();

            if (!validationResult)
            {
                return;
            }

            var folderCreationRequest = new FolderCreationRequestDto(_folderName, ParentFolderId);

            _processing = true;

            var response = await BlazFolderService.CreateNewFolderAsync(folderCreationRequest);

            if (response.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);

                await OnCreated.InvokeAsync(response.Data);

                _folderName = string.Empty;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _processing = false;
        }

        private bool ValidateTextInput()
        {
            _folderName = _folderName?.Trim();

            if (string.IsNullOrWhiteSpace(_folderName))
            {
                Snackbar.Add(@Resource.FolderNameCannotBeEmptyOrWhitespace, Severity.Warning);
                return false;
            }

            if (Regex.IsMatch(_folderName, @"[\/\\:\*\\""<>\?\|]"))
            {
                Snackbar.Add(@Resource.FolderNameContainsInvalidCharacters, Severity.Error);
                return false;
            }

            return true;
        }

        private async Task Close() => await OnCancelled.InvokeAsync();
    }
}
