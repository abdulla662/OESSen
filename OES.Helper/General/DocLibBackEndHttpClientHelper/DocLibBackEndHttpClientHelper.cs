using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OES.Helper.General.DocLibBackEndHttpClientHelper
{
    public class DocLibBackEndHttpClientHelper
    {
        public HttpClient HttpClient { get; }
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly string _commonRoutePrefix = "/api";

        public DocLibBackEndHttpClientHelper(IHttpClientFactory httpClientFactory, FilterParamsValues filterParamsValues)
        {
            HttpClient = httpClientFactory.CreateClient("DocLibApi");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            _filterParamsValues = filterParamsValues;
        }

        public async Task<TResponse> GetFromJsonAsync<TResponse>(string relativeUrl)
        {
            PrepareRequestHeaders(_filterParamsValues);

            return await HttpClient.GetFromJsonAsync<TResponse>($"{_commonRoutePrefix}/{relativeUrl}");
        }

        public async Task<TResponse> GetAsync<TResponse>(string relativeUrl, CancellationToken cancellationToken = default)
        {
            PrepareRequestHeaders(_filterParamsValues);

            var response = await HttpClient.GetAsync($"{_commonRoutePrefix}/{relativeUrl}", cancellationToken);

            return await HandleResponseAsync<TResponse>(response);
        }

        public async Task<TResponse> PostAsJsonAsync<TRequest, TResponse>(string relativeUrl, TRequest data)
        {
            PrepareRequestHeaders(_filterParamsValues);

            var response = await HttpClient.PostAsJsonAsync($"{_commonRoutePrefix}/{relativeUrl}", data, _jsonOptions);

            return await HandleResponseAsync<TResponse>(response);
        }

        public async Task<TResponse> PostMultipartAsync<TResponse>(string relativeUrl, MultipartFormDataContent formData, CancellationToken cancellationToken = default)
        {
            PrepareRequestHeaders(_filterParamsValues);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_commonRoutePrefix}/{relativeUrl}")
            {
                Content = formData
            };

            var response = await HttpClient.SendAsync(request, cancellationToken);

            return await HandleResponseAsync<TResponse>(response);
        }

        public async Task<TResponse> PutAsJsonAsync<TRequest, TResponse>(string relativeUrl, TRequest data)
        {
            PrepareRequestHeaders(_filterParamsValues);

            var response = await HttpClient.PutAsJsonAsync($"{_commonRoutePrefix}/{relativeUrl}", data, _jsonOptions);

            return await HandleResponseAsync<TResponse>(response);
        }

        public async Task<TResponse> DeleteFromJsonAsync<TResponse>(string relativeUrl)
        {
            PrepareRequestHeaders(_filterParamsValues);

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

        private void PrepareRequestHeaders(FilterParamsValues filterParamsValues)
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", filterParamsValues.CurrentBearerToken);
        }
    }
}
