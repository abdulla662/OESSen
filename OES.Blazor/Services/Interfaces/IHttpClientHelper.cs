using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces
{
    public interface IHttpClientHelper
    {
        public HttpClient _httpClient { get; set; }

        Task<ApiResponse> GetAsync<T>(string Url, CancellationToken cancellationToken = default);

        Task<ApiResponse> PostAsync(object T, string Url, CancellationToken cancellationToken = default);

        Task<ApiResponse> PostMultipartAsJsonAsync<T>(T dto, string url, CancellationToken cancellationToken = default) where T : class;

        Task<ApiResponse> PostFileWithJsonAsync<TDto>(IBrowserFile file, TDto dto, string url, long maxFileBytes = 1024 * 1024 * 50, CancellationToken cancellationToken = default) where TDto : class;

        Task<ApiResponse> PutAsync(object T, string Url, CancellationToken cancellationToken = default);

        Task<ApiResponse> DeleteAsync(string urlWithParams, CancellationToken cancellationToken = default);
    }
}