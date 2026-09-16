using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Net;

namespace OES.Blazor.Dialogs.Venue
{
    public partial class ImportVenuesDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IRowMapper _rowMapper { get; set; }
        [Inject] public IBlazVenueService _venueService { get; set; }
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

        private MemoryStream stream = new MemoryStream();
        private List<AddVenueRequestDto> dataTable = [];
        private IList<IBrowserFile> files = new List<IBrowserFile>();
        private MudFileUpload<IBrowserFile> fileUploaderRef;
        private List<string> ErrorListDto = new List<string>();
        private readonly List<string> requiredHeaders = typeof(AddVenueRequestDto).GetProperties().Select(p => p.Name).ToList();
        private List<AddVenueRequestDto> addMultipleVenueDto = new();

        private void DeleteFile(IBrowserFile file)
        {
            files.Remove(file);
            stream.Position = 0;
            stream.Seek(0, SeekOrigin.Begin);
            fileUploaderRef.ClearAsync();
            dataTable.Clear();
            ErrorListDto.Clear();
            StateHasChanged();
        }

        private async Task HandleFileSelected(IBrowserFile file)
        {
            if (!files.Any())
            {
                if (file != null)
                {
                    files.Add(file);

                    ErrorListDto.Clear();

                    await using (var stream = new MemoryStream())
                    {
                        await file.OpenReadStream().CopyToAsync(stream);

                        stream.Position = 0;

                        string extension = Path.GetExtension(file.Name).ToLower();

                        switch (extension)
                        {
                            case ".csv":
                                ReadCsvFile(stream);
                                break;
                            case ".xls":
                            case ".xlsx":
                                ReadExcelFile(stream);
                                break;
                            default:
                                Snackbar.Add(Resource.Unsupportedfileformat, Severity.Error);
                                break;
                        }
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
            ErrorListDto.Clear();
            int currentRow = 1;

            while (!reader.EndOfStream)
            {
                currentRow++;
                var line = reader.ReadLine();

                if (line == null) continue;

                var values = line.Split(',');

                var dto = _rowMapper.MapRowToDto<AddVenueRequestDto>(headers, values);

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
                        ErrorListDto.Add($"{Resource.row} {currentRow}: {validationResult.ErrorMessage}");
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

            if (ErrorListDto.Any())
            {
                Snackbar.Add($"{ErrorListDto.Count} {Resource.validationerrorsinthefileChecktheerrorslist}", Severity.Error);
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
            ErrorListDto.Clear();

            for (int row = 2; row <= rowCount; row++)
            {
                var values = new string[colCount];

                for (int col = 1; col <= colCount; col++)
                {
                    values[col - 1] = worksheet.Cells[row, col].Text;
                }

                var dto = _rowMapper.MapRowToDto<AddVenueRequestDto>(headers, values);

                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

                if (!isValid)
                {
                    foreach (var validationResult in validationResults)
                    {
                        ErrorListDto.Add($"{Resource.row} {row - 1}:  {validationResult.ErrorMessage}");
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

            if (ErrorListDto.Any())
            {
                Snackbar.Add($"{ErrorListDto.Count} {Resource.validationerrorsinthefileChecktheerrorslist}", Severity.Error);
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
            if (ErrorListDto.Any())
            {
                Snackbar.Add(Resource.Pleasefixtheexistingerrorsanduploadthefileagain, Severity.Error);
                StateHasChanged();
                return;
            }
            if (dataTable.Count > 0)
            {
                var duplicateValidation = ValidateDuplicateRecords(dataTable);
                if (duplicateValidation.HasDuplicates)
                {
                    ErrorListDto.AddRange(duplicateValidation.ErrorMessages);
                    Snackbar.Add(Resource.ThereareduplicaterowsinthefileChecktheerrorslist, Severity.Error);
                    StateHasChanged();
                    return;
                }
            }

            addMultipleVenueDto = dataTable;

            if (addMultipleVenueDto.Count > 0)
            {
                var res = await _venueService.AddMultipleVenueAsync(new AddMultipleVenuesRequestDto(addMultipleVenueDto));

                if (res.StatusCode == HttpStatusCode.Accepted)
                {
                    SplitResponseMessage(res.Message, Severity.Warning);
                }
                else if (res.StatusCode != HttpStatusCode.OK)
                {
                    SplitResponseMessage(res.Message, Severity.Error);
                }
                else
                {
                    Snackbar.Add(res.Message, Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
            }
            else
            {
                Snackbar.Add(Resource.UploadFileIsRequired, Severity.Error);
            }

            StateHasChanged();
        }

        private static (bool HasDuplicates, List<string> ErrorMessages) ValidateDuplicateRecords(List<AddVenueRequestDto> venues)
        {
            var errorMessages = new List<string>();
            var hasDuplicates = false;

            var nameGroups = venues
                .Where(v => !string.IsNullOrWhiteSpace(v.Name))
                .GroupBy(v => v.Name.Trim().ToLower())
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in nameGroups)
            {
                hasDuplicates = true;
                var duplicateNames = string.Join(", ", group.Select(v => $"'{v.Name}'"));
                errorMessages.Add($"{Resource.VenueNameAlreadyExists}: {duplicateNames}");
            }

            var codeGroups = venues
                .Where(v => !string.IsNullOrWhiteSpace(v.Code))
                .GroupBy(v => v.Code.Trim().ToLower())
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in codeGroups)
            {
                hasDuplicates = true;
                var duplicateCodes = string.Join(", ", group.Select(v => $"'{v.Code}'"));
                errorMessages.Add($"{Resource.Duplicatevenuecodesfound}: {duplicateCodes}");
            }
            return (hasDuplicates, errorMessages);
        }

        private void SplitResponseMessage(string fullMessage, Severity severity)
        {
            if (string.IsNullOrWhiteSpace(fullMessage))
            {
                Snackbar.Add(Resource.AnUnExpectedErrorOccurred, severity);
                return;
            }

            int rowStartIndex = fullMessage.IndexOf(Resource.row, StringComparison.Ordinal);

            string summaryMessage;
            List<string> errorMessages = new();

            if (rowStartIndex > 0)
            {
                summaryMessage = fullMessage[..rowStartIndex].Trim().TrimEnd(':');

                string errorDetails = fullMessage[rowStartIndex..].TrimEnd(';').Trim();

                if (!string.IsNullOrWhiteSpace(errorDetails))
                {
                    errorMessages = errorDetails
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .ToList();
                }
            }
            else
            {
                summaryMessage = fullMessage.Trim();
            }

            Snackbar.Add(summaryMessage, severity);

            if (errorMessages.Count > 0)
                ErrorListDto.AddRange(errorMessages);
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
