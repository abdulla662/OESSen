using System.Net.Http.Json;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.DocLibHttpClientHelperService
{
    public class DocLibHttpClientHelper
    {
        public HttpClient HttpClient { get; }
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _commonRoutePrefix = "/api";


        public DocLibHttpClientHelper(IHttpClientFactory httpClientFactory)
        {
            HttpClient = httpClientFactory.CreateClient("DocLibApi");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task<TResponse> GetFromJsonAsync<TResponse>(string relativeUrl)
        {
            return await HttpClient.GetFromJsonAsync<TResponse>($"{_commonRoutePrefix}/{relativeUrl}");
        }

        public async Task<TResponse> PostAsJsonAsync<TRequest, TResponse>(string relativeUrl, TRequest data)
        {
            var response = await HttpClient.PostAsJsonAsync($"{_commonRoutePrefix}/{relativeUrl}", data, _jsonOptions);

            return await HandleResponseAsync<TResponse>(response);
        }

        public async Task<TResponse> PutAsJsonAsync<TRequest, TResponse>(string relativeUrl, TRequest data)
        {
            var response = await HttpClient.PutAsJsonAsync($"{_commonRoutePrefix}/{relativeUrl}", data, _jsonOptions);

            return await HandleResponseAsync<TResponse>(response);
        }

        public async Task<TResponse> DeleteFromJsonAsync<TResponse>(string relativeUrl)
        {
            var response = await HttpClient.DeleteAsync($"{_commonRoutePrefix}/{relativeUrl}");

            return await HandleResponseAsync<TResponse>(response);
        }


        public async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            }

            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException($"Error: {response.StatusCode} - {error}");
        }
    }
}
