using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.FileManager;
using OES.Blazor.Services.Interfaces.DocumentService;
using OES.Blazor.Services.Interfaces.FolderService;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Helper.Dtos.Document.Request;
using OES.Helper.Dtos.Document.Response;
using OES.Helper.Dtos.Folder.Request;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Reflection.Metadata;

namespace OES.Blazor.Components.Common.FileManagerComponent
{
    public partial class FileManagerComponent
    {
        [Inject] private IBlazFolderService BlazFolderService { get; set; } = default!;

        [Inject] private IBlazDocumentService BlazDocumentService { get; set; } = default!;

        [Inject] private IBlazQuestionMetaData BlazQuestionMetaData { get; set; } = default!;

        [Inject] private IDialogService DialogService { get; set; } = default!;

        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [Inject] private ILogger<FileManagerComponent> Logger { get; set; } = default!;

        [Parameter] public bool FileSelectionEnabled { get; set; }

        [Parameter] public EventCallback<FolderDocumentListResponseDto> ReceiveSelectedFile { get; set; }

        [Parameter] public int Elevation { get; set; } = 25;


        public string SearchKey { get; set; }

        private List<FolderDocumentListResponseDto> _folderDocumentListDtos = [];

        private Stack<FolderDocumentListResponseDto> _foldersNavigationStack = new();

        private FolderDocumentListResponseDto _currentFolder;

        private FolderDocumentListResponseDto _selectedFileForDialog;

        private Dictionary<Guid, bool> _downloadStates = new();

        private int _currentPageNumber = 1;

        private bool _areThereMorePages;

        private bool _processing;

        private bool _processingMoving;

        private bool _processingCopy;

        private bool isVisible = false;

        private bool _searchModeEnabled;

        private CurrentFolderStatus _currentFolderStatus = CurrentFolderStatus.CheckingContent;

        private Guid selectedFile_FolderId;

        private HashSet<Guid> isOpen = new();

        private bool _openedFirstFolder = false;

        private bool _showCreateFolderInline = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var status = await GetFoldersAndDocumentsListAsync(_currentFolder);

                _currentFolderStatus = status;

                await TryOpenFirstFolderAsync();
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "Document Library service is currently unavailable.");
            }
        }


        private async Task TryOpenFirstFolderAsync()
        {
            if (_openedFirstFolder) return;

            if (_currentFolderStatus == CurrentFolderStatus.HasContent &&
                _folderDocumentListDtos?.Any(x => x.IsFolder) == true &&
                _foldersNavigationStack.Count == 0
            )
            {
                _openedFirstFolder = true;
                var firstFolder = _folderDocumentListDtos.First(x => x.IsFolder);
                await OpenFolderAsync(firstFolder);
            }
        }


        private void ToggleMenu(Guid fileId)
        {
            BlazDocumentService.ClearCutDocument();
            BlazDocumentService.SetCutDocument(fileId); // Seems to mean by SetCutDocument -> SetCurrentSelectedDocument

            if (!isOpen.Add(fileId))
            {
                isOpen.Remove(fileId);
            }

            StateHasChanged();
        }


        private void CreateNewFolderToggle()
        {
            _showCreateFolderInline = !_showCreateFolderInline;
        }


        private async Task OnFolderCreatedAsync(FolderCreationResponseDto data)
        {
            _showCreateFolderInline = false;

            _currentFolderStatus = CurrentFolderStatus.HasContent;

            _folderDocumentListDtos.Insert(0, new FolderDocumentListResponseDto(data.Id, data.Name, null, null, true));
            _folderDocumentListDtos = [.. _folderDocumentListDtos.OrderByDescending(x => x.IsFolder)];

            await InvokeAsync(StateHasChanged);
        }


        private void OnCreateFolderCancelled()
        {
            _showCreateFolderInline = false;
        }


        private async Task ViewFileAsync(FolderDocumentListResponseDto folderDocumentListResponseDto)
        {
            var parameters = new DialogParameters<DocumentViewDialog>
            {
                { x => x.Document, folderDocumentListResponseDto }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true
            };

            await DialogService.ShowAsync<DocumentViewDialog>(string.Empty, parameters, options);
        }


        private async Task SaveDocumentIdAsync(Guid fileId)
        {
            isVisible = true;

            selectedFile_FolderId = _currentFolder.Id;

            BlazDocumentService.SetCutDocument(fileId);
        }


        private async Task MoveDocumentAsync(Guid newFolderId)
        {
            Guid fileId = (Guid)BlazDocumentService.GetCutDocument();

            if (fileId == newFolderId)
            {
                Snackbar.Add(Resource.chooseAnotherLocation, Severity.Error);

                throw new InvalidOperationException();
            }

            _processingMoving = true;

            var documentMoveRequestDto = new DocumentMoveRequestDto(DocumentId: fileId, NewFolderId: newFolderId);

            var response = await BlazDocumentService.DocumentMoveAsync(documentMoveRequestDto);

            if (response.Success)
            {
                BlazDocumentService.ClearCutDocument();

                isVisible = false;

                _folderDocumentListDtos.Clear();

                _currentFolderStatus = CurrentFolderStatus.CheckingContent;

                var status = await GetFoldersAndDocumentsListAsync(_currentFolder);

                _currentFolderStatus = status;

                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _processingMoving = false;

            await InvokeAsync(StateHasChanged);
        }


        private async Task CopyDocumentAsync(Guid newFolderId)
        {
            Guid fileId = (Guid)BlazDocumentService.GetCutDocument();

            _processingCopy = true;

            var documentMoveRequestDto = new DocumentMoveRequestDto(DocumentId: fileId, NewFolderId: newFolderId);

            var response = await BlazDocumentService.DocumentCopyAsync(documentMoveRequestDto);

            if (response.Success)
            {
                BlazDocumentService.ClearCutDocument();

                isVisible = false;

                _folderDocumentListDtos.Clear();

                _currentFolderStatus = CurrentFolderStatus.CheckingContent;

                var status = await GetFoldersAndDocumentsListAsync(_currentFolder);

                _currentFolderStatus = status;

                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _processingCopy = false;

            await InvokeAsync(StateHasChanged);
        }


        private async Task CancelMoving()
        {
            isVisible = false;

            BlazDocumentService.ClearCutDocument();
        }


        private async Task DeleteFolderAsync(Guid folderId)
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<FolderDeletionDialog>(string.Empty, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is true)
            {
                var response = await BlazFolderService.DeleteFolderAsync(folderId);

                if (response.Success)
                {
                    _folderDocumentListDtos.RemoveAll(result => result.Id == folderId);

                    if (_folderDocumentListDtos.Count == 0)
                        _currentFolderStatus = CurrentFolderStatus.NoContent;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }


        private async Task DeleteFileAsync(Guid documentId)
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<DocumentDeletionDialog>(@Resource.DeleteConfirmation, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is true)
            {
                var isUsedInQuestion = await BlazQuestionMetaData.IsTheDocumentUsedInAnyQuestionAsync(documentId);

                if (isUsedInQuestion.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(Resource.ThisDocumentIsAlreadyUsedInQuestionsAndCannotBeDeleted, Severity.Error);
                    return;
                }

                var response = await BlazDocumentService.DeleteDocumentAsync(documentId);

                if (response.Success)
                {
                    _folderDocumentListDtos.RemoveAll(x => x.Id == documentId);

                    if (_folderDocumentListDtos.Count == 0)
                        _currentFolderStatus = CurrentFolderStatus.NoContent;

                    Snackbar.Add(@Resource.DocumentDeletedSuccessfully, Severity.Success);
                }
                else
                {
                    Snackbar.Add(@Resource.FailedToDeleteDocument, Severity.Error);
                }

                StateHasChanged();
            }
        }


        public async Task DownloadFileAsync(Guid documentId)
        {
            _downloadStates[documentId] = true;

            StateHasChanged();

            Snackbar.Add(@Resource.DownloadHasStarted, Severity.Info);

            var response = await BlazDocumentService.DownloadDocumentAsync(documentId);

            if (response.Success && response.Data != null)
            {
                var fileResult = response.Data;
                var byteArray = fileResult.FileContents;
                var base64Content = Convert.ToBase64String(byteArray);
                var dataUri = $"data:{fileResult.ContentType};base64,{base64Content}";

                await JSRuntime.InvokeVoidAsync(
                    "buildDynamicAnchorElement",
                    fileResult.FileDownloadName,
                    dataUri
                );

                Snackbar.Add(@Resource.DownloadCompleted, Severity.Success);
            }
            else
            {
                Snackbar.Add(Resource.FailedToDownloadFilePleaseTryAgain, Severity.Error);
            }

            _downloadStates[documentId] = false;

            StateHasChanged();
        }


        private async Task OpenFolderAsync(FolderDocumentListResponseDto dto)
        {
            if (dto.IsFolder)
            {
                _foldersNavigationStack.Push(dto);

                StateHasChanged();

                _folderDocumentListDtos = [];

                _currentPageNumber = 1;

                _currentFolderStatus = CurrentFolderStatus.CheckingContent;

                var status = await GetFoldersAndDocumentsListAsync(dto);

                _currentFolderStatus = status;
            }
        }


        private async Task ViewMoreAsync()
        {
            _currentPageNumber++;

            _processing = true;

            await GetFoldersAndDocumentsListAsync(_currentFolder);

            _processing = false;
        }


        private async Task NavigateToFolderThroughBreadcrumbAsync(BreadcrumbItem targetFolderBreadcrumb)
        {
            var targetFolder = new FolderDocumentListResponseDto(Guid.Parse(targetFolderBreadcrumb.Href), targetFolderBreadcrumb.Text, null, null, true); // ParentFolderId is null here because we don't know and don't need it

            if (_foldersNavigationStack.Any(folder => folder.Id == targetFolder.Id))
            {
                while (_foldersNavigationStack.Peek().Id != targetFolder.Id)
                {
                    _foldersNavigationStack.Pop();
                }

                _foldersNavigationStack.Pop();

                await OpenFolderAsync(targetFolder);
            }
        }


        private async Task UploadDocumentAsync()
        {
            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
            };

            var dialogParams = new DialogParameters<DocumentUploadDialog>
            {
                { x => x.CurrentFolderId, _currentFolder.Id }
            };

            var dialog = await DialogService.ShowAsync<DocumentUploadDialog>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var data = (DocumentMetadataResponseDto)result.Data;

                _currentFolderStatus = CurrentFolderStatus.HasContent;

                var firstFileIndex = _folderDocumentListDtos.FindIndex(x => !x.IsFolder);

                if (firstFileIndex == -1)
                {
                    _folderDocumentListDtos.Add(new FolderDocumentListResponseDto(data.Id, data.Name, data.Type, data.Size, false));
                }
                else
                {
                    _folderDocumentListDtos.Insert(firstFileIndex, new FolderDocumentListResponseDto(data.Id, data.Name, data.Type, data.Size, false));
                }

                StateHasChanged();
            }
        }


        private async Task<CurrentFolderStatus> GetFoldersAndDocumentsListAsync(FolderDocumentListResponseDto currentFolder)
        {
            _currentFolder = currentFolder;

            var paginatedList = await BlazFolderService.GetFoldersAndDocumentsListAsync(_currentFolder?.Id, _currentPageNumber);

            if (paginatedList.Items.Count > 0)
            {
                _areThereMorePages = paginatedList.HasNextPage;

                _folderDocumentListDtos.AddRange(paginatedList.Items);

                _folderDocumentListDtos = [.. _folderDocumentListDtos.OrderByDescending(x => x.IsFolder)];

                return CurrentFolderStatus.HasContent;
            }

            _areThereMorePages = false;

            return CurrentFolderStatus.NoContent;
        }


        public async Task OnTableSearchAsync()
        {
            _searchModeEnabled = true;

            _currentPageNumber = 1;

            _folderDocumentListDtos.Clear();

            StateHasChanged();

            if (string.IsNullOrWhiteSpace(SearchKey?.ToString()))
            {
                _currentFolderStatus = CurrentFolderStatus.CheckingContent;

                var status = await GetFoldersAndDocumentsListAsync(_currentFolder);

                _currentFolderStatus = status;

                _searchModeEnabled = false;
            }
            else
            {
                var paginatedList = await BlazFolderService.SearchFolderAndDocumentListAsync(SearchKey, _currentPageNumber);

                if (paginatedList.Items.Count > 0)
                {
                    _currentFolderStatus = CurrentFolderStatus.HasContent;

                    _areThereMorePages = paginatedList.HasNextPage;

                    _folderDocumentListDtos.AddRange(paginatedList.Items);
                }
                else
                {
                    _currentFolderStatus = CurrentFolderStatus.NoContent;

                    _areThereMorePages = false;
                }
            }

            StateHasChanged();
        }


        public async Task ResetSearchAsync()
        {
            SearchKey = string.Empty;

            _currentPageNumber = 1;

            _folderDocumentListDtos.Clear();

            _currentFolderStatus = CurrentFolderStatus.CheckingContent;

            var status = await GetFoldersAndDocumentsListAsync(_currentFolder);

            _currentFolderStatus = status;

            _searchModeEnabled = false;

            StateHasChanged();
        }


        private async Task OpenDialogOfNameUpdateAsync(FolderDocumentListResponseDto currentFolder)
        {
            var dialogOptions = new DialogOptions
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium
            };

            var dialogParams = new DialogParameters<NameUpdateDialog>
            {
                { x => x.CurrentItemName, currentFolder.Name }
            };

            var dialog = DialogService.Show<NameUpdateDialog>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var newName = result.Data.ToString();

                await UpdateItemNameAsync(currentFolder, newName);
            }

            StateHasChanged();
        }


        private async Task UpdateItemNameAsync(FolderDocumentListResponseDto currentFolder, string newName)
        {
            if (currentFolder.IsFolder)
            {
                var folderEditRequest = new FolderUpdateRequestDto(
                    currentFolder.Id,
                    newName,
                    _currentFolder?.Id
                );

                var response = await BlazFolderService.UpdateFolderNameAsync(folderEditRequest);

                if (response.Success)
                {
                    currentFolder.Name = newName;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }
            else
            {
                var DocumentEditRequest = new DocumentUpdateRequestDto(
                    currentFolder.Id,
                    newName,
                    _currentFolder.Id
                );

                var response = await BlazDocumentService.UpdateDocumentNameAsync(DocumentEditRequest);

                if (response.Success)
                {
                    currentFolder.Name = newName;

                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }
            }
        }


        private void CaptureSelectedFile(FolderDocumentListResponseDto selectedItem)
        {
            if (!selectedItem.IsFolder)
            {
                _selectedFileForDialog = selectedItem;
            }
            else
            {
                _selectedFileForDialog = null;
            }

            StateHasChanged();
        }


        private async Task ExposeSelectedFileOutsideAsync()
        {
            await ReceiveSelectedFile.InvokeAsync(_selectedFileForDialog);
        }
    }
}
