using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Interface.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace OES.Services.Services
{
    public class AddMultipleCandidateDtoValidator : IFileProcessingValidator<AddMultipleCandidateDto>
    {
        private readonly HashSet<string> _processedEmails = [];
        private readonly HashSet<string> _processedNationalIds = [];

        public List<ExcelValidationErrorDto> Validate(AddMultipleCandidateDto dto, int rowNumber)
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

            if (string.IsNullOrWhiteSpace(dto.NationalId))
            {
                errors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    CandidateIdentifier = dto.Name ?? "N/A",
                    FieldName = "National Id",
                    Value = "",
                    ErrorMessage = "National Id is a required field and cannot be empty."
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Mobile))
            {
                errors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    CandidateIdentifier = dto.Email ?? dto.Name ?? "N/A",
                    FieldName = "Mobile",
                    Value = " ",
                    ErrorMessage = " mobile number is required."
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.CandidateCode) && !_processedNationalIds.Add(dto.CandidateCode))
            {
                errors.Add(new ExcelValidationErrorDto
                {
                    RowNumber = rowNumber,
                    CandidateIdentifier = dto.Email ?? dto.Name ?? "N/A",
                    FieldName = "CandidateCode",
                    Value = dto.CandidateCode,
                    ErrorMessage = $"Duplicate Candidate Code '{dto.CandidateCode}' found in the file."
                });
            }

            return errors;
        }

        public void ResetValidationState()
        {
            _processedEmails.Clear();
            _processedNationalIds.Clear();
        }
    }
}