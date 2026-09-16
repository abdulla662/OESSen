using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Data;
using System.Net;
using System.Text;

namespace OES.Services.Services
{
    public class StandaloneQuestionFileProcessingService(ICommonService _commonService)
    {
        private const int ErrorThreshold = 100;

        private readonly HashSet<string> _seenCodes = new(StringComparer.OrdinalIgnoreCase);

        public async Task<ApiResponse> ProcessFileAsync(IFormFile file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (fileExtension is not (".xls" or ".xlsx"))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    string.Format(Resource.Unsupportedfileformat, fileExtension)
                );
            }

            if (!await FileSignatures.HasValidSignatureAsync(file))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.UnsupportedMediaType,
                    string.Format(Resource.InvalidFileFormat, fileExtension)
                );
            }

            _seenCodes.Clear();

            var errors = new List<StandaloneQuestionExcelValidationErrorDto>();
            var validRows = new List<UpdateStandaloneQuestionDto>();

            await foreach (var dto in ReadRowsAsync(file, errors))
                validRows.Add(dto);

            if (errors.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationFailed,
                    errors.Take(ErrorThreshold).ToList()
                );
            }

            var codes = validRows.Select(r => r.QuestionCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var questionDictionary = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(q => codes.Contains(q.Code))
                .AsNoTracking()
                .ToDictionaryAsync(q => q.Code, q => q);

            var notFoundErrors = new List<StandaloneQuestionExcelValidationErrorDto>();

            foreach (var row in validRows)
            {
                if (!questionDictionary.ContainsKey(row.QuestionCode))
                {
                    notFoundErrors.Add(new StandaloneQuestionExcelValidationErrorDto(row.RowNumber, Resource.QuestionCode, Resource.QuestionNotFound, row.QuestionCode));
                }
            }

            if (notFoundErrors.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationFailed,
                    notFoundErrors.Take(ErrorThreshold).ToList()
                );
            }

            var questionIds = questionDictionary.Values.Select(q => q.Id).ToList();

            var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (questionIds.Count > 0)
            {
                var parameters = new List<(string Name, object Value)> { ("pQuestionIds", System.Text.Json.JsonSerializer.Serialize(questionIds)) };
                var result = await _commonService._unitOfWork.ExecuteStoredProcedureAsync<UsedQuestionIdDto>("CheckQuestionsUsedInPaper", parameters);
                var usedIds = result.Select(x => x.QuestionId).ToHashSet();
                usedCodes = questionDictionary.Where(kv => usedIds.Contains(kv.Value.Id)).Select(kv => kv.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var usageErrors = new List<StandaloneQuestionExcelValidationErrorDto>();
            foreach (var row in validRows)
            {
                if (usedCodes.Contains(row.QuestionCode))
                {
                    usageErrors.Add(new StandaloneQuestionExcelValidationErrorDto(row.RowNumber, Resource.QuestionCode, Resource.UsedInPaper, row.QuestionCode));
                }
            }

            if (usageErrors.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationFailed,
                    usageErrors.Take(ErrorThreshold).ToList()
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FileProcessedSuccessfully,
                validRows
            );
        }

        private async IAsyncEnumerable<UpdateStandaloneQuestionDto> ReadRowsAsync(IFormFile file, List<StandaloneQuestionExcelValidationErrorDto> errors)
        {
            await using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
            });

            var table = dataSet.Tables[0];
            if (table?.Rows.Count == 0) yield break;

            var headers = table.Columns.Cast<DataColumn>().Select(c => c.ToString() ?? string.Empty).ToArray();

            int rowNumber = 1;
            foreach (DataRow dataRow in table.Rows)
            {
                rowNumber++;
                var values = Enumerable.Range(0, table.Columns.Count)
                    .Select(i => dataRow[i]?.ToString()?.Trim() ?? string.Empty)
                    .ToArray();

                int idx = Array.FindIndex(headers, h => string.Equals(h?.Trim(), "QuestionCode", StringComparison.OrdinalIgnoreCase) || string.Equals(h?.Trim(), "Question Code", StringComparison.OrdinalIgnoreCase));
                var code = idx != -1 && idx < values.Length ? values[idx] : values.ElementAtOrDefault(0) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(code))
                {
                    errors.Add(new StandaloneQuestionExcelValidationErrorDto(rowNumber, Resource.QuestionCode, Resource.QuestionCodeRequired));
                    if (errors.Count >= ErrorThreshold)
                    {
                        errors.Add(new StandaloneQuestionExcelValidationErrorDto(rowNumber, null, string.Format(Resource.ProcessingStoppedExceededErrors, ErrorThreshold)));
                        yield break;
                    }
                    continue;
                }

                if (!_seenCodes.Add(code))
                {
                    errors.Add(new StandaloneQuestionExcelValidationErrorDto(rowNumber, Resource.QuestionCode, Resource.Duplicatequestioncodesfound, code));
                    if (errors.Count >= ErrorThreshold)
                    {
                        errors.Add(new StandaloneQuestionExcelValidationErrorDto(rowNumber, null, string.Format(Resource.ProcessingStoppedExceededErrors, ErrorThreshold), code));
                        yield break;
                    }
                    continue;
                }

                yield return new UpdateStandaloneQuestionDto(rowNumber, code);
            }
        }
    }
}
