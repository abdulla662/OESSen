using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Data;
using System.Globalization;
using System.Net;
using System.Text;

namespace OES.Services.Services
{
    public class QuestionDeltaFileProcessingService(ICommonService _commonService)
    {
        private const int ErrorThreshold = 100;

        private readonly HashSet<string> _seenCodes = new(StringComparer.OrdinalIgnoreCase);

        private static UpdateQuestionDeltaDto MapRow(string[] headers, string[] values, int rowNumber)
        {
            var deltaRaw = GetValue(headers, values, nameof(UpdateQuestionDeltaDto.DeltaValue));

            return new UpdateQuestionDeltaDto(
                rowNumber,
                GetValue(headers, values, nameof(UpdateQuestionDeltaDto.QuestionCode)),
                TryParseDelta(deltaRaw, out var deltaValue) ? deltaValue : null
            );
        }

        private List<QuestionDeltaExcelValidationErrorDto> Validate(UpdateQuestionDeltaDto item, int rowNumber)
        {
            var errors = new List<QuestionDeltaExcelValidationErrorDto>();

            if (string.IsNullOrWhiteSpace(item.QuestionCode))
            {
                errors.Add(new QuestionDeltaExcelValidationErrorDto(
                    rowNumber,
                    Resource.QuestionCode,
                    Resource.QuestionCodeRequired
                ));
            }

            if (!string.IsNullOrWhiteSpace(item.QuestionCode) && !_seenCodes.Add(item.QuestionCode))
            {
                errors.Add(new QuestionDeltaExcelValidationErrorDto(
                    rowNumber,
                    Resource.QuestionCode,
                    Resource.Duplicatequestioncodesfound,
                    item.QuestionCode
                ));
            }

            return errors;
        }

        public async Task<ApiResponse> ProcessFileAsync(IFormFile file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!await FileSignatures.HasValidSignatureAsync(file))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.UnsupportedMediaType,
                    string.Format(Resource.InvalidFileFormat, fileExtension)
                );
            }

            _seenCodes.Clear();

            var errors = new List<QuestionDeltaExcelValidationErrorDto>();
            var validRows = new List<UpdateQuestionDeltaDto>();

            await foreach (var dto in ReadRowsAsync(file, errors)) validRows.Add(dto);

            if (errors.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationFailed,
                    errors.Take(ErrorThreshold).ToList()
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FileProcessedSuccessfully,
                validRows
            );
        }

        private async IAsyncEnumerable<UpdateQuestionDeltaDto> ReadRowsAsync(IFormFile file, List<QuestionDeltaExcelValidationErrorDto> errors)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (fileExtension is not (".xls" or ".xlsx"))
            {
                errors.Add(new QuestionDeltaExcelValidationErrorDto(null, null, $"{Resource.Unsupportedfileformat}: {fileExtension}"));

                yield break;
            }

            await using var stream = file.OpenReadStream();

            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = dataSet.Tables[0];

            if (table?.Rows.Count == 0) yield break;

            var headers = table.Columns
                .Cast<DataColumn>()
                .Select(c => c.ToString() ?? string.Empty)
                .ToArray();

            int rowNumber = 1;

            foreach (DataRow dataRow in table.Rows)
            {
                rowNumber++;

                var values = Enumerable.Range(0, table.Columns.Count)
                    .Select(i => dataRow[i]?.ToString()?.Trim() ?? string.Empty)
                    .ToArray();

                var deltaRaw = GetValue(headers, values, nameof(UpdateQuestionDeltaDto.DeltaValue));
                var dto = MapRow(headers, values, rowNumber);
                var rowErrors = Validate(dto, rowNumber);

                if (string.IsNullOrWhiteSpace(deltaRaw))
                {
                    rowErrors.Add(new QuestionDeltaExcelValidationErrorDto(
                        rowNumber,
                        Resource.DeltaValue,
                        Resource.DeltaValueIsRequired,
                        dto.QuestionCode,
                        dto.DeltaValue
                    ));
                }
                else if (!TryParseDelta(deltaRaw, out var deltaValue))
                {
                    rowErrors.Add(new QuestionDeltaExcelValidationErrorDto(
                        rowNumber,
                        Resource.DeltaValue,
                        Resource.InvalidDeltaValue,
                        dto.QuestionCode,
                        dto.DeltaValue
                    ));
                }
                else if (deltaValue < 0 || deltaValue > 1)
                {
                    rowErrors.Add(new QuestionDeltaExcelValidationErrorDto(
                        rowNumber,
                        Resource.DeltaValue,
                        Resource.DeltaValueMustBeBetweenZeroAndOne,
                        dto.QuestionCode,
                        dto.DeltaValue
                    ));
                }

                if (rowErrors.Count > 0)
                {
                    errors.AddRange(rowErrors);

                    if (errors.Count >= ErrorThreshold)
                    {
                        errors.Add(new QuestionDeltaExcelValidationErrorDto(
                            rowNumber,
                            null,
                            string.Format(Resource.ProcessingStoppedExceededErrors, ErrorThreshold),
                            dto.QuestionCode,
                            dto.DeltaValue
                        ));

                        yield break;
                    }
                }
                else
                {
                    yield return dto;
                }
            }
        }

        #region Helper Methods

        private static string GetValue(string[] headers, string[] values, string name)
        {
            int idx = Array.FindIndex(
                headers,
                h => string.Equals(h?.Trim(), name, StringComparison.OrdinalIgnoreCase));

            return idx != -1 && idx < values.Length
                ? values[idx]?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static bool TryParseDelta(string value, out decimal result)
        {
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                   || decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                   || decimal.TryParse(value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        #endregion
    }
}