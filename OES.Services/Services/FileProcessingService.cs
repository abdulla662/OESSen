using CsvHelper;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.CheckDuplicationClass;
using OES.Helper.RegularExpressions;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Data;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace OES.Services.Services
{
    public class FileProcessingService<T> : IFileProcessingService<T>
    {
        private readonly Func<string[], string[], T> _mapRow;
        private readonly IFileProcessingValidator<T> _validator;
        private readonly ICommonService _commonService;

        private const int ErrorThreshold = 100;

        public FileProcessingService(
            Func<string[], string[], T> mapRow,
            ICommonService commonService,
            IFileProcessingValidator<T> validator
        )
        {
            _mapRow = mapRow ?? throw new ArgumentNullException(nameof(mapRow));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _commonService = commonService;
        }

        public async Task<ApiResponse> ProcessFileAsync(IFormFile file)
        {
            var validationErrors = new List<ExcelValidationErrorDto>();
            var validDtos = new List<T>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!await FileSignatures.HasValidSignatureAsync(file))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.UnsupportedMediaType,
                    string.Format(Resource.InvalidFileFormat, fileExtension),
                    null
                );
            }

            _validator.ResetValidationState();

            await foreach (var dto in ReadValidDtosAsync(file, validationErrors))
            {
                validDtos.Add(dto);
            }

            if (validationErrors.Count > 0)
            {
                int emptyCount = validationErrors.Count(e =>
                    e.ErrorMessage.Contains("empty", StringComparison.OrdinalIgnoreCase) ||
                    e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase)
                );

                int duplicateRegCount = validationErrors.Count(e =>
                    string.Equals((e.FieldName ?? string.Empty).Replace(" ", ""), nameof(DumpImportCandidateRequestDto.RegistrationNumber), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals((e.FieldName ?? string.Empty).Replace(" ", ""), "RegistrationNumber", StringComparison.OrdinalIgnoreCase)
                );

                int duplicateOtherCount = validationErrors.Count(e => e.ErrorMessage.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)) - duplicateRegCount;

                string message = Resource.ValidationFailed;

                var parts = new List<string>();

                if (emptyCount > 0) parts.Add($"{emptyCount} {Resource.RowsWithEmptyrRequired}");
                if (duplicateRegCount > 0) parts.Add($"{duplicateRegCount} {Resource.RowsWithDuplicateRegisterationNumber}");
                if (duplicateOtherCount > 0) parts.Add($"{duplicateOtherCount} {Resource.RowsWithDuplicateIdentifiers}");
                if (parts.Count > 0) message = $"{Resource.FileContains}" + string.Join($" {Resource.And} ", parts) + "";

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    message,
                    validationErrors.Take(ErrorThreshold).ToList()
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FileProcessedSuccessfully,
                validDtos
            );
        }

        private async IAsyncEnumerable<T> ReadValidDtosAsync(IFormFile file, List<ExcelValidationErrorDto> rowErrors)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            await using var fileStream = file.OpenReadStream();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (fileExtension == ".csv")
            {
                using var reader = new StreamReader(fileStream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                await csv.ReadAsync();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;
                int currentRow = 1;
                var seenRegNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenUserNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenNationalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                while (await csv.ReadAsync())
                {
                    currentRow++;

                    var values = headers.Select((_, i) => csv.GetField(i) ?? string.Empty).ToArray();

                    if (HasEmptyRequiredFields(headers, values, currentRow, rowErrors))
                    {
                        if (rowErrors.Count >= ErrorThreshold)
                            yield break;

                        continue;
                    }

                    if (HandleFormatConversions(headers, values, currentRow, rowErrors))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    if (ValidateNumericFields(headers, values, currentRow, rowErrors))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    if (CheckDuplicateIdentifiers(
                        headers,
                        values,
                        currentRow,
                        rowErrors,
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateRegistrationNumber,
                            seenRegNumbers,
                            nameof(DumpImportCandidateRequestDto.RegistrationNumber), "RegistrationNumber", "Registration No", "RegNo", "Reg No", "RegNumber"
                        ),
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateEmail,
                            seenEmails,
                            nameof(DumpImportCandidateRequestDto.Email), "Email", "E-mail", "Email Address"
                        ),
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateUserName,
                            seenUserNames,
                            nameof(DumpImportCandidateRequestDto.UserName), "UserName", "Username", "User Name"
                        ),
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateNationalId,
                            seenNationalIds,
                            nameof(DumpImportCandidateRequestDto.NationalId), "NationalId", "National ID", "NationalID", "NID"
                        )
                    ))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    var dto = _mapRow(headers, values);
                    var validationErrors = _validator.Validate(dto, currentRow);

                    if (validationErrors.Count > 0)
                    {
                        rowErrors.AddRange(validationErrors);
                        if (rowErrors.Count > ErrorThreshold)
                        {
                            rowErrors.Add(new ExcelValidationErrorDto { RowNumber = currentRow, ErrorMessage = string.Format(Resource.ProcessingStoppedExceededErrors, ErrorThreshold) });
                            yield break;
                        }
                    }
                    else
                    {
                        yield return dto;
                    }
                }
            }
            else if (fileExtension == ".xls" || fileExtension == ".xlsx")
            {
                using var reader = ExcelReaderFactory.CreateReader(fileStream);

                var result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration { UseHeaderRow = true }
                });

                var data = result.Tables[0];

                if (data?.Rows.Count == 0) yield break;

                var headers = data.Columns.Cast<DataColumn>().Select(c => c.ToString() ?? string.Empty).ToArray();
                int rowNumber = 1;
                var seenRegNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenUserNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var seenNationalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow dataRow in data.Rows)
                {
                    rowNumber++;

                    var values = Enumerable.Range(0, data.Columns.Count).Select(col =>
                    {
                        var cell = dataRow[col];
                        if (cell is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        return cell?.ToString() ?? string.Empty;
                    })
                    .ToArray();

                    if (HasEmptyRequiredFields(headers, values, rowNumber, rowErrors))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    if (HandleFormatConversions(headers, values, rowNumber, rowErrors))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    if (ValidateNumericFields(headers, values, rowNumber, rowErrors))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    if (ValidateArabicAndEmailFields(headers, values, rowNumber, rowErrors))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    if (CheckDuplicateIdentifiers(
                        headers,
                        values,
                        rowNumber,
                        rowErrors,
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateRegistrationNumber,
                            seenRegNumbers,
                            nameof(DumpImportCandidateRequestDto.RegistrationNumber), "RegistrationNumber", "Registration No", "RegNo", "Reg No", "RegNumber"
                        ),
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateEmail,
                            seenEmails,
                            nameof(DumpImportCandidateRequestDto.Email), "Email", "E-mail", "Email Address"
                        ),
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateUserName,
                            seenUserNames,
                            nameof(DumpImportCandidateRequestDto.UserName), "UserName", "Username", "User Name"
                        ),
                        new DuplicateFieldSpecDto(
                            Resource.DuplicateNationalId,
                            seenNationalIds,
                            nameof(DumpImportCandidateRequestDto.NationalId), "NationalId", "National ID", "NationalID", "NID"
                        )
                    ))
                    {
                        if (rowErrors.Count >= ErrorThreshold) yield break;
                        continue;
                    }

                    var dto = _mapRow(headers, values);
                    var validationErrors = _validator.Validate(dto, rowNumber);

                    if (validationErrors.Count > 0)
                    {
                        rowErrors.AddRange(validationErrors);

                        if (rowErrors.Count > ErrorThreshold)
                        {
                            rowErrors.Add(new ExcelValidationErrorDto { RowNumber = rowNumber, ErrorMessage = string.Format(Resource.ProcessingStoppedExceededErrors, ErrorThreshold) });
                            yield break;
                        }
                    }
                    else
                    {
                        yield return dto;
                    }
                }
            }
            else
            {
                rowErrors.Add(new ExcelValidationErrorDto { ErrorMessage = $"{Resource.Unsupportedfileformat}: {fileExtension}" });
            }
        }

        private static bool ValidateNumericFields(string[] headers, string[] values, int rowNumber, List<ExcelValidationErrorDto> rowErrors)
        {
            bool hasError = false;

            int mobileColumnIndex = Array.FindIndex(headers, h => h?.Replace(" ", "").Equals("Mobile", StringComparison.OrdinalIgnoreCase) ?? false);

            if (mobileColumnIndex != -1 && mobileColumnIndex < values.Length)
            {
                string mobileValue = values[mobileColumnIndex]?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(mobileValue))
                {
                    bool isGeneralMobileNumber = Regex.IsMatch(mobileValue, RegularExpressions.GeneralMobileNumber);

                    if (!isGeneralMobileNumber)
                    {
                        rowErrors.Add(new ExcelValidationErrorDto
                        {
                            RowNumber = rowNumber,
                            FieldName = headers[mobileColumnIndex],
                            ErrorMessage = string.Format(Resource.InvalidMobileFormat, headers[mobileColumnIndex], mobileValue)
                        });
                        hasError = true;
                    }
                }
            }

            int regNumIdx = FindHeaderIndexInsensitive(headers, nameof(DumpImportCandidateRequestDto.RegistrationNumber), "RegistrationNumber", "Registration No", "RegNo", "Reg No", "RegNumber");

            if (regNumIdx != -1 && regNumIdx < values.Length)
            {
                string regNumValue = values[regNumIdx]?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(regNumValue))
                {
                    if (!long.TryParse(regNumValue, out _))
                    {
                        rowErrors.Add(new ExcelValidationErrorDto
                        {
                            RowNumber = rowNumber,
                            FieldName = headers[regNumIdx],
                            ErrorMessage = string.Format(Resource.InvalidRegisterationNumberFormat, Resource.RegistrationNumber, regNumValue)
                        });
                        hasError = true;
                    }
                }
            }

            //int nationalIdColumnIndex = Array.FindIndex(headers, h => h?.Replace(" ", "").Equals("NationalId", StringComparison.OrdinalIgnoreCase) ?? false);

            //if (nationalIdColumnIndex != -1 && nationalIdColumnIndex < values.Length)
            //{
            //    string nationalIdValue = values[nationalIdColumnIndex]?.Trim() ?? string.Empty;

            //    if (!string.IsNullOrWhiteSpace(nationalIdValue))
            //    {
            //        bool isEgyptianNationalId = Regex.IsMatch(nationalIdValue, RegularExpressions.EgyptianNationalId);
            //        bool isSaudiNationalId = Regex.IsMatch(nationalIdValue, RegularExpressions.SaudiNationalId);

            //        if (!isEgyptianNationalId && !isSaudiNationalId)
            //        {
            //            rowErrors.Add(new ExcelValidationErrorDto
            //            {
            //                RowNumber = rowNumber,
            //                FieldName = headers[nationalIdColumnIndex],
            //                ErrorMessage = string.Format(Resource.InvalidNationalIdFormat, headers[nationalIdColumnIndex], nationalIdValue)
            //            });
            //            hasError = true;
            //        }
            //    }
            //}

            return hasError;
        }

        private static int FindHeaderIndexInsensitive(string[] headers, params string[] candidates)
        {
            var normHeaders = headers.Select(Normalize).ToArray();

            var normCandidates = candidates.Select(Normalize).ToArray();

            for (int i = 0; i < normHeaders.Length; i++)
            {
                foreach (var c in normCandidates)
                {
                    if (normHeaders[i] == c) return i;
                }
            }

            return -1;
        }

        private static bool CheckDuplicateIdentifiers(string[] headers, string[] values, int rowNumber, List<ExcelValidationErrorDto> rowErrors, params DuplicateFieldSpecDto[] specs)
        {
            bool hasAnyDuplicate = false;

            int emailIdx = FindHeaderIndexInsensitive(headers, "Email", "E-mail", "Email Address");
            string emailValue = emailIdx != -1 && emailIdx < values.Length ? values[emailIdx]?.Trim() : string.Empty;

            foreach (var spec in specs)
            {
                int idx = FindHeaderIndexInsensitive(headers, spec.FieldNames);

                if (idx == -1) continue;

                var raw = values[idx]?.Trim();

                if (string.IsNullOrWhiteSpace(raw)) continue;

                if (spec.Seen.Contains(raw))
                {
                    rowErrors.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = rowNumber,
                        FieldName = headers[idx],
                        Value = raw,
                        CandidateIdentifier = !string.IsNullOrEmpty(emailValue) ? emailValue : raw,
                        ErrorMessage = string.IsNullOrEmpty(spec.ErrorMessage)
                            ? $"{headers[idx]} {Resource.Duplicate}"
                            : spec.ErrorMessage
                    });

                    hasAnyDuplicate = true;
                }
                else
                {
                    spec.Seen.Add(raw);
                }
            }

            return hasAnyDuplicate;
        }

        private static readonly HashSet<string> OptionalFields = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(AddMultipleCandidateDto.DateOfBirth)
        };

        private static bool HasEmptyRequiredFields(string[] headers, string[] values, int rowNumber, List<ExcelValidationErrorDto> rowErrors)
        {
            bool hasEmpty = false;

            for (int i = 0; i < values.Length; i++)
            {
                if (OptionalFields.Contains(headers[i]))
                    continue;

                if (string.IsNullOrWhiteSpace(values[i]))
                {
                    rowErrors.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = rowNumber,
                        FieldName = headers[i],
                        Value = "",
                        ErrorMessage = string.Format(Resource.FieldRequiredEmpty, headers[i])
                    });
                    hasEmpty = true;
                }
            }

            if (hasEmpty && rowErrors.Count >= ErrorThreshold)
            {
                rowErrors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    ErrorMessage = string.Format(Resource.ProcessingStoppedExceededErrors, ErrorThreshold)
                });
            }

            return hasEmpty;
        }

        private static bool HandleFormatConversions(string[] headers, string[] values, int rowNumber, List<ExcelValidationErrorDto> rowErrors)
        {
            bool hasFormatError = false;

            int genderIndex = Array.FindIndex(headers, h => h.ToLower() == nameof(AddMultipleCandidateDto.Gender).ToLower());

            if (genderIndex != -1 && !string.IsNullOrWhiteSpace(values[genderIndex]))
            {
                string lowerCaseValue = values[genderIndex].ToLower();

                switch (lowerCaseValue)
                {
                    case "m":
                    case "male":
                        values[genderIndex] = "0";
                        break;

                    case "f":
                    case "female":
                        values[genderIndex] = "1";
                        break;

                    default:
                        if (!int.TryParse(values[genderIndex], out int genderInt) || (genderInt != 0 && genderInt != 1))
                        {
                            rowErrors.Add(new ExcelValidationErrorDto
                            {
                                RowNumber = rowNumber,
                                FieldName = headers[genderIndex],
                                Value = values[genderIndex],
                                ErrorMessage = string.Format(Resource.GenderInvalid, values[genderIndex])
                            });
                            hasFormatError = true;
                        }
                        break;
                }
            }


            int dobIndex = Array.FindIndex(headers, h => h.ToLower() == nameof(AddMultipleCandidateDto.DateOfBirth).ToLower());

            if (dobIndex != -1 && !string.IsNullOrWhiteSpace(values[dobIndex]) && !DateTime.TryParse(values[dobIndex], out _))
            {
                rowErrors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    FieldName = headers[dobIndex],
                    Value = values[dobIndex],
                    ErrorMessage = string.Format("DateOfBirthInvalid {0}", values[dobIndex])
                });
                hasFormatError = true;
            }


            int regDateTimeIndex = Array.FindIndex(headers, h => h.ToLower() == nameof(AddMultipleCandidateDto.RegistrationDateTime).ToLower());

            if (regDateTimeIndex != -1 && !string.IsNullOrWhiteSpace(values[regDateTimeIndex]) && !DateTime.TryParse(values[regDateTimeIndex], out _))
            {
                rowErrors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    FieldName = headers[regDateTimeIndex],
                    Value = values[regDateTimeIndex],
                    ErrorMessage = string.Format("RegistrationDateInvalid {0}", values[regDateTimeIndex])
                });
                hasFormatError = true;
            }


            string normalizedVenueCodeHeader = nameof(DumpImportCandidateRequestDto.VenueCode).ToLower();

            int venueCodeIndex = Array.FindIndex(headers, h => (h?.ToLower().Replace(" ", "") ?? "") == normalizedVenueCodeHeader);

            if (venueCodeIndex != -1 && venueCodeIndex < values.Length)
            {
                string venueValue = values[venueCodeIndex]?.Trim();

                if (string.IsNullOrWhiteSpace(venueValue))
                {
                    rowErrors.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = rowNumber,
                        FieldName = headers[venueCodeIndex],
                        Value = venueValue,
                        ErrorMessage = string.Format(Resource.VenueCodeInvalid, venueValue)
                    });
                    hasFormatError = true;
                }
            }


            int examDateIndex = Array.FindIndex(headers, h => h.ToLower().Replace(" ", "") == nameof(DumpImportCandidateRequestDto.CandidateExamDate).ToLower());

            if (examDateIndex >= 0 && !string.IsNullOrWhiteSpace(values[examDateIndex]) && !DateTime.TryParse(values[examDateIndex], out _))
            {
                rowErrors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    FieldName = headers[examDateIndex],
                    Value = values[examDateIndex],
                    ErrorMessage = string.Format("CandidateExamDateInvalid {0}", values[examDateIndex])
                });
                hasFormatError = true;
            }

            return hasFormatError;
        }

        public async IAsyncEnumerable<T> ProcessWithoutValidation(IFormFile file)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            await using var fileStream = file.OpenReadStream();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (fileExtension == ".csv")
            {
                using var reader = new StreamReader(fileStream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                await csv.ReadAsync();
                csv.ReadHeader();
                var headers = csv.HeaderRecord;
                int currentRow = 1;

                while (await csv.ReadAsync())
                {
                    currentRow++;
                    var values = headers.Select((_, i) => csv.GetField(i) ?? string.Empty).ToArray();
                    var dto = _mapRow(headers, values);
                    yield return dto;
                }
            }
            else if (fileExtension == ".xls" || fileExtension == ".xlsx")
            {
                using var reader = ExcelReaderFactory.CreateReader(fileStream);

                var result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration { UseHeaderRow = true }
                });

                var data = result.Tables[0];

                if (data?.Rows.Count == 0) yield break;

                var headers = data.Columns.Cast<DataColumn>().Select(c => c.ToString() ?? string.Empty).ToArray();
                int rowNumber = 1;

                foreach (DataRow dataRow in data.Rows)
                {
                    rowNumber++;

                    var values = Enumerable.Range(0, data.Columns.Count).Select(col =>
                    {
                        var cell = dataRow[col];
                        if (cell is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        return cell?.ToString() ?? string.Empty;
                    }).ToArray();

                    var rowErrors = new List<ExcelValidationErrorDto>();

                    HandleFormatConversions(headers, values, rowNumber, rowErrors);

                    var dto = _mapRow(headers, values);

                    yield return dto;
                }
            }
        }

        static string Normalize(string s) => (s ?? string.Empty).Replace(" ", "").ToLower();

        private static bool ValidateArabicAndEmailFields(string[] headers, string[] values, int rowNumber, List<ExcelValidationErrorDto> rowErrors)
        {
            bool hasError = false;

            var arabicCheckFields = new (string ResourceKey, string[] HeaderNames)[]
            {
                (nameof(DumpImportCandidateRequestDto.CandidateCode), new[] { "CandidateCode", "Candidate Code" }),
                (nameof(DumpImportCandidateRequestDto.UserName), new[] { "UserName", "User Name" })
            };

            foreach (var (ResourceKey, HeaderNames) in arabicCheckFields)
            {
                int idx = FindHeaderIndexInsensitive(headers, HeaderNames);

                if (idx == -1 || idx >= values.Length) continue;

                string value = values[idx]?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, RegularExpressions.ArabicCharactersExpression))
                {
                    rowErrors.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = rowNumber,
                        FieldName = headers[idx],
                        Value = value,
                        ErrorMessage = string.Format(Resource.FieldMustNotContainArabicCharacters, headers[idx])
                    });
                    hasError = true;
                }
            }

            int emailIdx = FindHeaderIndexInsensitive(headers, "Email", "E-mail", "Email Address");

            if (emailIdx != -1 && emailIdx < values.Length)
            {
                string emailValue = values[emailIdx]?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(emailValue))
                {
                    bool containsArabic = Regex.IsMatch(emailValue, RegularExpressions.ArabicCharactersExpression);
                    bool isValidFormat = Regex.IsMatch(emailValue, RegularExpressions.EmailExpression);

                    if (containsArabic || !isValidFormat)
                    {
                        rowErrors.Add(new ExcelValidationErrorDto
                        {
                            RowNumber = rowNumber,
                            FieldName = headers[emailIdx],
                            Value = emailValue,
                            ErrorMessage = string.Format(Resource.EmailIsInvalidOrContainsArabicCharacters, emailValue)
                        });
                        hasError = true;
                    }
                }
            }

            return hasError;
        }
    }
}
