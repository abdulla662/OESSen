using OES.Helper.Dtos.Candidate;

namespace OES.Interface.Interfaces
{
    public interface IFileProcessingValidator<T>
    {
        List<ExcelValidationErrorDto> Validate(T item, int rowNumber);

        void ResetValidationState();
    }
}