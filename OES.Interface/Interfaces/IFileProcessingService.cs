using Microsoft.AspNetCore.Http;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IFileProcessingService<T>
    {
        Task<ApiResponse> ProcessFileAsync(IFormFile file);

        IAsyncEnumerable<T> ProcessWithoutValidation(IFormFile file);
    }
}