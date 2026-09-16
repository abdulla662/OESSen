using Microsoft.AspNetCore.Components.Forms;
using OES.Blazor.Services.Implementation.Common;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation
{
    public class HttpClientHelper : IHttpClientHelper
    {
        public HttpClient _httpClient { get; set; }
        private readonly IApiResponse _apiResponse;
        private readonly IConfiguration _configuration;
        private readonly IBlazSessionStorageService _sessionStorageService;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public HttpClientHelper(IHttpClientFactory httpClientFactory,
                                IApiResponse apiResponse,
                                IConfiguration configuration,
                                IBlazSessionStorageService sessionStorageService)
        {
            _httpClient = httpClientFactory.CreateClient("OesApi");
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
            _apiResponse = apiResponse;
            _configuration = configuration;
            _sessionStorageService = sessionStorageService;
        }

        public async Task<ApiResponse> GetAsync<T>(string Url, CancellationToken cancellationToken = default)
        {
            var url = $"{CentralizedUrlHelper.OesApiBaseUrl}{Url}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await SendAndHandleEncryptionAsync(request, cancellationToken);

            return DeserializeDataField<T>(response);
        }

        public async Task<ApiResponse> PostAsync(object T, string Url, CancellationToken cancellationToken = default)
        {
            var validationError = ValidateNoLeadingTrailingSpaces(T);

            if (validationError != null)
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.BadRequest, validationError);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{CentralizedUrlHelper.OesApiBaseUrl}{Url}")
            {
                Content = new StringContent(JsonSerializer.Serialize(T), Encoding.UTF8, MiscConstants.ApplicationJsonContentType)
            };

            return await SendAndHandleEncryptionAsync(request, cancellationToken);
        }

        public async Task<ApiResponse> PostMultipartAsJsonAsync<T>(T dto, string url, CancellationToken cancellationToken = default) where T : class
        {
            using var formData = new MultipartFormDataContent();

            if (dto is IBrowserFile dtofile)
            {
                var fileContent = new StreamContent(dtofile.OpenReadStream(1024 * 1024 * 50, cancellationToken));

                fileContent.Headers.ContentType = new MediaTypeHeaderValue(dtofile.ContentType);

                formData.Add(fileContent, "file", dtofile.Name);
            }
            else
            {
                foreach (var property in typeof(T).GetProperties())
                {
                    var value = property.GetValue(dto);

                    if (value == null) continue;

                    if (property.PropertyType == typeof(IBrowserFile))
                    {
                        var file = (IBrowserFile)value;

                        var fileContent = new StreamContent(file.OpenReadStream(1024 * 1024 * 50, cancellationToken));

                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                        formData.Add(fileContent, property.Name, file.Name);
                    }
                    else if (value is IEnumerable<long> values)
                    {
                        foreach (var item in values)
                        {
                            formData.Add(new StringContent(item.ToString()), property.Name);
                        }
                    }
                    else
                    {
                        formData.Add(new StringContent(value.ToString(), Encoding.UTF8), property.Name);
                    }
                }
            }

            var response = await _httpClient.PostAsync($"{CentralizedUrlHelper.OesApiBaseUrl}{url}", formData, cancellationToken);

            response.EnsureSuccessStatusCode();

            var res = JsonSerializer.Deserialize<ApiResponse>(await response.Content.ReadAsStringAsync(), _jsonOptions);

            return res ?? _apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                "Something went wrong. Please try again later.");
        }

        public async Task<ApiResponse> PostFileWithJsonAsync<TDto>(IBrowserFile file, TDto dto, string url, long maxFileBytes = 1024 * 1024 * 50, CancellationToken cancellationToken = default) where TDto : class
        {
            using var formData = new MultipartFormDataContent();

            if (file != null)
            {
                var fileContent = new StreamContent(file.OpenReadStream(maxFileBytes, cancellationToken));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                formData.Add(fileContent, "file", file.Name);
            }

            if (dto != null)
            {
                var json = JsonSerializer.Serialize(dto);
                formData.Add(new StringContent(json, Encoding.UTF8, "application/json"), "dto"); // NOTE: The name of the receiving parameter in the corresponding controller endpoint should be also "dto".
            }

            var response = await _httpClient.PostAsync($"{CentralizedUrlHelper.OesApiBaseUrl}{url}", formData, cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<ApiResponse>(
                await response.Content.ReadAsStringAsync(cancellationToken),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return result ?? _apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.SomethingWentWrong
            );
        }

        public async Task<ApiResponse> PutAsync(object T, string Url, CancellationToken cancellationToken = default)
        {
            var validationError = ValidateNoLeadingTrailingSpaces(T);

            if (validationError != null)
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.BadRequest, validationError);

            var request = new HttpRequestMessage(HttpMethod.Put, $"{CentralizedUrlHelper.OesApiBaseUrl}{Url}")
            {
                Content = new StringContent(JsonSerializer.Serialize(T), Encoding.UTF8, MiscConstants.ApplicationJsonContentType)
            };

            return await SendAndHandleEncryptionAsync(request, cancellationToken);
        }

        public async Task<ApiResponse> DeleteAsync(string urlWithParams, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{CentralizedUrlHelper.OesApiBaseUrl}{urlWithParams}");

            return await SendAndHandleEncryptionAsync(request, cancellationToken);
        }

        #region Shared Send/Encryption Helpers

        private async Task<ApiResponse> SendAndHandleEncryptionAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            bool encryptionEnabled = _configuration.GetValue<bool>(MiscConstants.PayloadEncryptionEnabled, false);

            bool developerDisabledEncryption = await IsDeveloperEncryptionDisabledAsync();

            bool isExcluded = EncryptionExclusionRoutes.IsExcluded(request.RequestUri?.ToString() ?? string.Empty);

            if (!encryptionEnabled || developerDisabledEncryption || isExcluded)
            {
                if (developerDisabledEncryption)
                {
                    request.Headers.TryAddWithoutValidation(MiscConstants.DeveloperDisableEncryption, "true");
                }

                // Encryption off (or excluded) — send as-is and deserialize directly
                var normalResponse = await _httpClient.SendAsync(request, cancellationToken);
                return await HandlePlainResponseAsync(normalResponse, cancellationToken);
            }

            // Encrypt the request body (POST/PUT) if present; GET has no body
            if (request.Content != null)
            {
                var plainJson = await request.Content.ReadAsStringAsync(cancellationToken);
                var encryptedContent = AesCipher.Encrypt(plainJson);
                request.Content = new StringContent(encryptedContent, Encoding.UTF8, MiscConstants.ApplicationJsonContentType);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, response.StatusCode, "Request failed!");

            var encryptedResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrEmpty(encryptedResponse))
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, "Empty response!");

            var decryptedResponse = AesCipher.Decrypt(encryptedResponse);

            var result = JsonSerializer.Deserialize<ApiResponse>(decryptedResponse, _jsonOptions);

            return result ?? _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, "Error deserializing the response!");
        }

        private async Task<ApiResponse> HandlePlainResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, response.StatusCode, "Request failed!");

            if (string.IsNullOrEmpty(responseContent))
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, "Empty response!");

            var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, _jsonOptions);

            return result ?? _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, "Error deserializing the response!");
        }

        private ApiResponse DeserializeDataField<T>(ApiResponse response)
        {
            if (response?.Data != null)
            {
                var result = JsonSerializer.Deserialize<T>(response.Data.ToString() ?? "", _jsonOptions);
                response.Data = result;
            }
            return response;
        }

        #endregion

        #region Helper Methods

        private static string ValidateNoLeadingTrailingSpaces(object obj)
        {
            if (obj == null) return null;

            foreach (var property in obj.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                var value = property.GetValue(obj) as string;

                // if (value != null && value != value.Trim())
                //    return $"{Resource.NoLeadingTrailingSpaces}";

                if (!string.IsNullOrEmpty(value) && property.Name.Contains(nameof(System.Net.Mail), StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        var addr = new System.Net.Mail.MailAddress(value);

                        if (addr.Address != value || char.IsDigit(value[0]))
                            return $"'{property.Name}' {Resource.EmailFormatInvalid}";
                    }
                    catch
                    {
                        return $"'{property.Name}' {Resource.EmailFormatInvalid}";
                    }
                }
            }

            return null;
        }

        public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            return keyValuePairs?.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty)) ?? [];
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private async Task<bool> IsDeveloperEncryptionDisabledAsync()
        {
            try
            {
                var value = await _sessionStorageService.GetValue<string>(MiscConstants.DeveloperDisableEncryption, WithEncrypt: false);

                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        #endregion Helper Methods
    }
}