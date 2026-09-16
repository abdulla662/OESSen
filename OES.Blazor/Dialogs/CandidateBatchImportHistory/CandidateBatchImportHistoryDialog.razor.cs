using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.CandidateBatchImportHistory;
using OES.Helper.Dtos.ScheduleCandidate;
using OES.Helper.Dtos.ScheduleCandidate.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Text.Json;

namespace OES.Blazor.Dialogs.CandidateBatchImportHistory
{
    public partial class CandidateBatchImportHistoryDialog
    {
        [Inject] private IBlazCandidateBatchImportHistoryService BlazCandidateBatchImportHistoryService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [CascadingParameter] public MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public long PaperId { get; set; }
        [Parameter] public string PaperName { get; set; } = string.Empty;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private bool _firstLoad = true;
        private bool _busy;

        public async Task<CustomTableData<GetCandidateBatchImportHistoryPaginationDto>> GetCandidateBatchImportHistoryAsync(PaginationSearchModel paginationSearchModel)
        {
            paginationSearchModel.PaginationOff = false;

            if (_firstLoad)
            {
                paginationSearchModel.OrderBy = "CreationDate DESC";
                _firstLoad = false;
            }

            if (string.IsNullOrWhiteSpace(paginationSearchModel.OrderBy))
                paginationSearchModel.OrderBy = "CreationDate DESC";

            paginationSearchModel.FilterObj ??= PaperId;

            var response = await BlazCandidateBatchImportHistoryService.GetAllPaginatedBatchesAsync(paginationSearchModel);

            if (response?.Data is not JsonElement je || je.ValueKind != JsonValueKind.Object)
                return new CustomTableData<GetCandidateBatchImportHistoryPaginationDto>([], 0);

            var data = je.Deserialize<PaginatedResponse<GetCandidateBatchImportHistoryPaginationDto>>(_jsonSerializerOptions);

            return new CustomTableData<GetCandidateBatchImportHistoryPaginationDto>
            {
                Items = data?.Items ?? [],
                TotalItems = data?.TotalItems ?? 0
            };
        }

        private async Task DownloadImportedExcelAsync(GetCandidateBatchImportHistoryPaginationDto dto)
        {
            if (dto is null)
            {
                Snackbar.Add(Resource.InvalidFileReference, Severity.Error);
                return;
            }

            if (dto.DataSource == DataSource.Excel)
            {
                var fileUrl = $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={dto.FileId}";
                await JSRuntime.InvokeVoidAsync("downloadFileFromUrl", fileUrl);
            }
            else
            {
                var fileBytes = await BlazCandidateBatchImportHistoryService.ExportBatchCandidatesAsync(dto.Id);

                if (fileBytes is null)
                {
                    Snackbar.Add(Resource.SomethingWentWrong, Severity.Error);
                    return;
                }

                var date = DateTime.Now.ToString("yyyy-MM-dd");
                var fileName = $"CBT Sync - {PaperName} - {date}.xlsx";

                await JSRuntime.InvokeVoidAsync("downloadFileFromBytes", fileBytes, fileName);
            }

            Snackbar.Add(Resource.FileDownloadedSuccessfully, Severity.Success);
        }

        private async Task ReverseBatchAsync(GetCandidateBatchImportHistoryPaginationDto getCandidateBatchImportHistoryPaginationDto)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, Resource.AreYouSureYouWantToReverseThisBatch },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Reverse }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                if (getCandidateBatchImportHistoryPaginationDto == null)
                {
                    Snackbar.Add(Resource.InvalidFileReference, Severity.Error);
                    return;
                }

                try
                {
                    _busy = true;

                    var response = await BlazCandidateBatchImportHistoryService.ReverseBatchAsync(getCandidateBatchImportHistoryPaginationDto);

                    if (response.CustomCodeStatus == CustomCodeStatus.Success)
                    {
                        getCandidateBatchImportHistoryPaginationDto.IsReversed = true;
                        Snackbar.Add(response.Message, Severity.Success);
                        MudDialog.Close(DialogResult.Ok(true));
                    }
                    else
                    {
                        Snackbar.Add(response.Message, Severity.Error);
                    }
                }
                finally
                {
                    _busy = false;
                    StateHasChanged();
                }
            }
        }

        public void Close() => MudDialog?.Close();
    }
}
