using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Interface.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace OES.Services.Services
{
    public class DumpImportCandidateRequestDtoValidator : IFileProcessingValidator<DumpImportCandidateRequestDto>
    {
        private readonly HashSet<string> _processedEmails = [];
        private readonly HashSet<string> _processedPhoneNumbers = [];
        private readonly HashSet<string> _processedNationalIds = [];
        private readonly HashSet<string> _processedRegistrationNumbers = [];

        public List<ExcelValidationErrorDto> Validate(DumpImportCandidateRequestDto dto, int rowNumber)
        {
            var errors = new List<ExcelValidationErrorDto>();

            var validationContext = new ValidationContext(dto, serviceProvider: null, items: null);

            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    string fieldName = validationResult.MemberNames.FirstOrDefault() ?? "UnknownField";

                    errors.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = rowNumber,
                        CandidateIdentifier = dto.Email ?? dto.NationalId ?? "N/A",
                        FieldName = fieldName,
                        Value = dto.GetType().GetProperty(fieldName)?.GetValue(dto)?.ToString() ?? "N/A",
                        ErrorMessage = validationResult.ErrorMessage
                    });
                }
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                errors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    CandidateIdentifier = dto.Email ?? "N/A",
                    FieldName = "Name",
                    Value = "",
                    ErrorMessage = "Name is a required field and cannot be empty."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                errors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    CandidateIdentifier = dto.Name ?? "N/A",
                    FieldName = "Email",
                    Value = "",
                    ErrorMessage = "Email is a required field and cannot be empty."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.RegistrationNumber))
            {
                errors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    CandidateIdentifier = dto.Email ?? dto.Name ?? "N/A",
                    FieldName = "RegistrationNumber",
                    Value = "",
                    ErrorMessage = "Registration Number is required."
                });
            }

            return errors;
        }

        public void ResetValidationState()
        {
            _processedEmails.Clear();
            _processedPhoneNumbers.Clear();
            _processedNationalIds.Clear();
            _processedRegistrationNumbers.Clear();
        }
    }
}