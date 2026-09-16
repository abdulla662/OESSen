using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Paper.ImportQuestionsRequestDto;
using OES.Helper.Dtos.Question;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.Paper.ImportQuestionsWithItemBanksDialog
{
    public partial class ImportQuestionsWithItemBanksDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IRowMapper RowMapper { get; set; }
        [Inject] public IBlazQuestionService QuestionService { get; set; }
        [Inject] public GlobalUserContext GlobalUserContext { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public bool PaperAllowInstantResult { get; set; }
        [Parameter] public int CurrentQuestionsCount { get; set; }

        private readonly MemoryStream stream = new();
        private readonly List<AddQuestionRequestDto> dataTable = [];
        private readonly IList<IBrowserFile> files = [];
        private MudFileUpload<IBrowserFile> fileUploaderRef;
        private readonly List<string> errorListDto = [];
        private readonly List<string> requiredHeaders = [.. typeof(AddQuestionRequestDto).GetProperties().Select(p => p.Name)];
        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private List<AddQuestionRequestDto> addMultipleQuestionsDto = [];

        private void DeleteFile(IBrowserFile file)
        {
            files.Remove(file);
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            fileUploaderRef.ClearAsync();
            dataTable.Clear();
            errorListDto.Clear();
            StateHasChanged();
        }

        private async Task HandleFileSelected(IBrowserFile file)
        {
            if (!files.Any())
            {
                if (file != null)
                {
                    files.Add(file);

                    errorListDto.Clear();

                    await using var _stream = new MemoryStream();

                    await file.OpenReadStream().CopyToAsync(_stream);

                    _stream.Position = 0;

                    string extension = Path.GetExtension(file.Name).ToLower();

                    switch (extension)
                    {
                        case ".csv":
                            ReadCsvFile(_stream);
                            break;
                        case ".xls":
                        case ".xlsx":
                            ReadExcelFile(_stream);
                            break;
                        default:
                            Snackbar.Add(Resource.Unsupportedfileformat, Severity.Error);
                            break;
                    }
                }
            }
            else
            {
                Snackbar.Add(Resource.Afilehasalreadybeenuploaded, Severity.Error);
            }
        }

        public void ReadCsvFile(Stream stream)
        {
            using var reader = new StreamReader(stream);

            var headerLine = reader.ReadLine();

            if (string.IsNullOrEmpty(headerLine))
            {
                Snackbar.Add(Resource.Noheaderfoundinfile, Severity.Error);
                return;
            }

            var headers = headerLine.Split(',');

            if (!requiredHeaders.TrueForAll(h => headers.Contains(h, StringComparer.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.Missingrequiredcolumnsinfile, Severity.Error);
                return;
            }

            dataTable.Clear();
            errorListDto.Clear();
            int currentRow = 1;

            while (!reader.EndOfStream)
            {
                currentRow++;
                var line = reader.ReadLine();

                if (line == null) continue;

                var values = line.Split(',');

                var dto = RowMapper.MapRowToDto<AddQuestionRequestDto>(headers, values);

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                bool isValid = Validator.TryValidateObject(
                    dto,
                    validationContext,
                    validationResults,
                    validateAllProperties: true
                );

                if (!isValid)
                {
                    foreach (var validationResult in validationResults)
                    {
                        errorListDto.Add($"{Resource.row} {currentRow}: {validationResult.ErrorMessage}");
                    }
                }

                dataTable.Add(dto);
            }

            if (dataTable.Count == 0)
            {
                Snackbar.Add(Resource.ThereisnodatainthefileEditthefileandtryagain, Severity.Warning);
                StateHasChanged();
                return;
            }

            if (errorListDto.Count > 0)
            {
                Snackbar.Add($"{errorListDto.Count} {Resource.validationerrorsinthefileChecktheerrorslist}", Severity.Error);
                StateHasChanged();
            }
        }

        public void ReadExcelFile(Stream stream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                Snackbar.Add(Resource.NoworksheetfoundinExcelfile, Severity.Error);
                return;
            }

            int rowCount = worksheet.Dimension?.Rows ?? 0;
            int colCount = worksheet.Dimension?.Columns ?? 0;
            var headers = new string[colCount];

            for (int col = 1; col <= colCount; col++)
            {
                headers[col - 1] = worksheet.Cells[1, col].Text;
            }

            if (!requiredHeaders.TrueForAll(h => headers.Contains(h, StringComparer.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.ErrorreadingExcelfilemissingrequiredheaders, Severity.Error);
                return;
            }

            dataTable.Clear();
            errorListDto.Clear();

            for (int row = 2; row <= rowCount; row++)
            {
                var values = new string[colCount];

                for (int col = 1; col <= colCount; col++)
                {
                    values[col - 1] = worksheet.Cells[row, col].Text;
                }

                var dto = RowMapper.MapRowToDto<AddQuestionRequestDto>(headers, values);

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

                if (!isValid)
                {
                    foreach (var validationResult in validationResults)
                    {
                        errorListDto.Add($"{Resource.row} {row - 1}:  {validationResult.ErrorMessage}");
                    }
                }

                dataTable.Add(dto);
            }

            if (dataTable.Count == 0)
            {
                Snackbar.Add(Resource.ThereisnodatainthefileEditthefileandtryagain, Severity.Warning);
                StateHasChanged();
                return;
            }

            if (errorListDto.Count > 0)
            {
                Snackbar.Add($"{errorListDto.Count} {Resource.validationerrorsinthefileChecktheerrorslist}", Severity.Error);
                StateHasChanged();
            }
        }

        private async Task SubmitSelection()
        {
            StateHasChanged();

            if (dataTable.Count == 0)
            {
                Snackbar.Add(Resource.ThereisnodatainthefileEditthefileandtryagain, Severity.Warning);
                StateHasChanged();
                return;
            }

            if (errorListDto.Count > 0)
            {
                Snackbar.Add(Resource.Pleasefixtheexistingerrorsanduploadthefileagain, Severity.Error);
                StateHasChanged();
                return;
            }

            if (dataTable.Count > 0)
            {
                var (HasDuplicates, ErrorMessages) = ValidateDuplicateRecords(dataTable);

                if (HasDuplicates)
                {
                    errorListDto.AddRange(ErrorMessages);
                    Snackbar.Add(Resource.ThereareduplicaterowsinthefileChecktheerrorslist, Severity.Error);
                    StateHasChanged();
                    return;
                }
            }

            addMultipleQuestionsDto = dataTable;

            var userId = GlobalUserContext.UserId.ToString();

            if (addMultipleQuestionsDto.Count > 0)
            {
                var apiResponse = await QuestionService.ValidateQuestionsByQuestionCodes(new AddMultipleQuestionsRequestDto(userId, addMultipleQuestionsDto, PaperAllowInstantResult));

                if (apiResponse.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(apiResponse.Message, Severity.Success);

                    var questionIdsAndItemBankIdsDto = JsonSerializer.Deserialize<QuestionIdsAndItemBankIdsDto>(apiResponse.Data.ToString(), jsonSerializerOptions);

                    var totalQuestions = questionIdsAndItemBankIdsDto.ValidateExcelSheetQuestionsDto.QuestionWithSectionNameDtos.Sum(q => q.SubQuestionsCount);

                    if (totalQuestions != CurrentQuestionsCount)
                    {
                        errorListDto.AddRange([Resource.NewImportedQuestionsCountDoesNotMatchPaperQuestionsCount]);
                        Snackbar.Add(Resource.NewImportedQuestionsCountDoesNotMatchPaperQuestionsCount, Severity.Error);
                        StateHasChanged();
                        return;
                    }

                    MudDialog.Close(DialogResult.Ok(questionIdsAndItemBankIdsDto));
                }
                else
                {
                    DisplayResponseErrorMessage(apiResponse.Message, Severity.Error);
                }
            }
            else
            {
                Snackbar.Add(Resource.UploadFileIsRequired, Severity.Error);
            }

            StateHasChanged();
        }

        private static (bool HasDuplicates, List<string> ErrorMessages) ValidateDuplicateRecords(List<AddQuestionRequestDto> questions)
        {
            var errorMessages = new List<string>();
            var hasDuplicates = false;

            var nameGroups = questions
                .Where(v => !string.IsNullOrWhiteSpace(v.QuestionCode))
                .GroupBy(v => v.QuestionCode.Trim().ToLower())
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in nameGroups)
            {
                hasDuplicates = true;
                var duplicateQuestionCodes = string.Join(", ", group.Select(v => $"'{v.QuestionCode}'"));
                errorMessages.Add($"{Resource.Duplicatequestioncodesfound}: {duplicateQuestionCodes}");
            }

            return (hasDuplicates, errorMessages);
        }

        private void DisplayResponseErrorMessage(string fullMessage, Severity severity)
        {
            if (string.IsNullOrWhiteSpace(fullMessage))
            {
                Snackbar.Add(Resource.AnUnExpectedErrorOccurred, severity);
                return;
            }

            Snackbar.Add(fullMessage, Severity.Error);

            errorListDto.Add(fullMessage);
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
