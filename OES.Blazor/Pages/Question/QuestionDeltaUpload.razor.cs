using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QuestionMetaData;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.Question
{
    public partial class QuestionDeltaUpload
    {
        [Inject] private IBlazQuestionMetaData QuestionMetadataService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private IFormFile? _formFile;
        private string _fileName = string.Empty;
        private string _fileSize = string.Empty;
        private bool _isValidating;
        private bool _isUpdating;
        private bool _showWarningDialog;
        private QuestionDeltaValidationResponseDto? _validationResponse;
        private const int MaxFileSizeInMB = 10;
        private const long MaxFileSizeBytes = MaxFileSizeInMB * 1024 * 1024;

        private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";
        private string _dragClass = DefaultDragClass;

        private string _errorSearchText = string.Empty;
        private IEnumerable<QuestionDeltaRowResultDto> _filteredErrorRows = [];

        private readonly DialogOptions _dialogOptions = new()
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = false,
            BackdropClick = false
        };

        private void FilterErrors(string? value)
        {
            _errorSearchText = value ?? string.Empty;

            _filteredErrorRows = _validationResponse?.Rows
                .Where(r => r.Status == QuestionDeltaRowStatus.Error &&
                            (string.IsNullOrWhiteSpace(_errorSearchText) || r.QuestionCode.Contains(_errorSearchText, StringComparison.OrdinalIgnoreCase)))
                .ToList()
            ?? [];
        }

        private void SetDragClass() => _dragClass = $"{DefaultDragClass} mud-border-primary";

        private void ClearDragClass() => _dragClass = DefaultDragClass;

        private async Task OnFileSelected(InputFileChangeEventArgs e)
        {
            ClearDragClass();
            _validationResponse = null;
            _errorSearchText = string.Empty;

            var browserFile = e.File;

            var extension = Path.GetExtension(browserFile.Name).ToLowerInvariant();

            if (extension is not (".xls" or ".xlsx"))
            {
                Snackbar.Add(Resource.UnsupportedFileFormat, Severity.Error);
                return;
            }

            if (browserFile.Size > MaxFileSizeBytes)
            {
                Snackbar.Add(string.Format(Resource.FileSizeShouldNotExceedInMB, MaxFileSizeInMB), Severity.Error);
                return;
            }

            var ms = new MemoryStream();
            await browserFile.OpenReadStream(MaxFileSizeBytes).CopyToAsync(ms);
            ms.Position = 0;

            _formFile = new FormFile(ms, 0, ms.Length, MiscConstants.File, browserFile.Name)
            {
                Headers = new HeaderDictionary(),
                ContentType = browserFile.ContentType
            };

            _fileName = browserFile.Name;
            _fileSize = $"{browserFile.Size / 1024.0 / 1024.0:F2} MB";
        }

        private void ClearFile()
        {
            _formFile = null;
            _fileName = string.Empty;
            _fileSize = string.Empty;
            _errorSearchText = string.Empty;
            _validationResponse = null;
            ClearDragClass();
        }

        private async Task ValidateAndUploadAsync()
        {
            if (_formFile is null)
            {
                Snackbar.Add(Resource.PleaseSlectFileFirst, Severity.Warning);
                return;
            }

            _isValidating = true;
            _validationResponse = null;
            _errorSearchText = string.Empty;

            try
            {
                var request = new UpdateQuestionDeltaRequestDto { File = _formFile };
                var response = await QuestionMetadataService.ValidateQuestionDeltaFromExcelAsync(request);

                if (response?.Data is null)
                {
                    Snackbar.Add(Resource.Error, Severity.Error);
                    return;
                }

                var validation = JsonSerializer.Deserialize<QuestionDeltaValidationResponseDto>(
                    response.Data.ToString()!,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (validation is null)
                {
                    Snackbar.Add(Resource.Error, Severity.Error);
                    return;
                }

                _validationResponse = validation;
                FilterErrors(_errorSearchText);

                if (validation.Rows.Any(r => r.Status == QuestionDeltaRowStatus.Error))
                {
                    Snackbar.Add(Resource.FileProcessingError, Severity.Error);
                    return;
                }

                if (!validation.CanProceed)
                {
                    Snackbar.Add(Resource.FileProcessingError, Severity.Error);
                    return;
                }

                if (validation.HasWarnings)
                {
                    _showWarningDialog = true;
                    StateHasChanged();
                    return;
                }

                await PerformUpdateAsync(validation.Rows);
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                _isValidating = false;
            }
        }

        private async Task ConfirmUpdateAsync()
        {
            if (_validationResponse is null) return;
            _showWarningDialog = false;
            await PerformUpdateAsync(_validationResponse.Rows);
        }

        private async Task PerformUpdateAsync(IEnumerable<QuestionDeltaRowResultDto> rows)
        {
            _isUpdating = true;

            try
            {
                var deltas = rows
                    .Where(r => r.Status != QuestionDeltaRowStatus.Error)
                    .Select(r => new UpdateQuestionDelta(r.QuestionCode, r.DeltaValue))
                    .ToList();

                var result = await QuestionMetadataService.UpdateQuestionDeltaFromExcel(new UpdateQuestionDeltaConfirmDto(deltas));

                if (result?.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(result.Message, Severity.Success);
                    ClearFile();
                }
                else
                {
                    Snackbar.Add(result?.Message ?? Resource.SomethingWentWrong, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}