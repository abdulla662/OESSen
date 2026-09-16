using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.Question.UploadFreeQuestionsDialog
{
    public partial class UploadFreeQuestionsDialog
    {
        [Inject] private IBlazQuestionService QuestionService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        private IFormFile? _formFile;
        private string _fileName = string.Empty;
        private string _fileSize = string.Empty;
        private bool _isValidating;
        private bool _isValid = false;
        private bool _isUpdating;
        private List<StandaloneQuestionExcelValidationErrorDto>? _validationResponse;
        private List<UpdateStandaloneQuestionDto> validRows = [];

        private const int MaxFileSizeInMB = 10;
        private const long MaxFileSizeBytes = MaxFileSizeInMB * 1024 * 1024;
        private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mt-4 mud-width-full mud-height-full";

        private string _dragClass = DefaultDragClass;
        private string _errorSearchText = string.Empty;
        private IEnumerable<StandaloneQuestionExcelValidationErrorDto> _filteredErrorRows = [];

        private void FilterErrors(string? value)
        {
            _errorSearchText = value ?? string.Empty;

            _filteredErrorRows = _validationResponse?
                .Where(r => r.ErrorMessage != null && (string.IsNullOrWhiteSpace(_errorSearchText) || r.QuestionCode.Contains(_errorSearchText, StringComparison.OrdinalIgnoreCase)))
                .ToList() ?? [];
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
                var request = new UploadFreeQuestions
                {
                    File = _formFile
                };

                var response = await QuestionService.ValidateStandaloneQuestionsCodes(request);

                if (response?.Data is null)
                {
                    Snackbar.Add(Resource.Error, Severity.Error);
                    return;
                }

                if (response.CustomCodeStatus == CustomCodeStatus.ValidationError)
                {
                    var errors = JsonSerializer.Deserialize<List<StandaloneQuestionExcelValidationErrorDto>>(
                        response.Data.ToString()!,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );

                    if (errors is null)
                    {
                        Snackbar.Add(Resource.Error, Severity.Error);
                        return;
                    }

                    _validationResponse = errors;

                    Snackbar.Add(Resource.FileProcessingError, Severity.Error);

                    FilterErrors(_errorSearchText);

                    _isValid = false;

                    return;
                }

                if (response.CustomCodeStatus == CustomCodeStatus.Success)
                {
                    validRows = JsonSerializer.Deserialize<List<UpdateStandaloneQuestionDto>>(
                       response.Data.ToString()!,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       }
                    );

                    if (validRows is null)
                    {
                        Snackbar.Add(Resource.Error, Severity.Error);
                        return;
                    }

                    Snackbar.Add(Resource.validationsuccessed, Severity.Success);

                    _isValid = true;

                    return;
                }

                Snackbar.Add(Resource.Error, Severity.Error);
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
            if (_validationResponse != null) return;
            await PerformUpdateAsync(validRows);
        }

        private async Task PerformUpdateAsync(List<UpdateStandaloneQuestionDto> rows)
        {
            _isUpdating = true;

            try
            {
                var selectedCodes = rows.ConvertAll(q => q.QuestionCode);

                var dto = new UpdateQuestionsCreationStatusRequestDto(
                    selectedCodes,
                    QuestionStatus.LayoutSelectedAndPending
                );

                var result = await QuestionService.ChangeQuestionsCreationStatusAsync(dto);

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

        private void Cancel() => MudDialog.Cancel();
    }
}
