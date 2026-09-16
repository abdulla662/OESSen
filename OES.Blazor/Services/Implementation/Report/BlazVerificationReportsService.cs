using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Results;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.Report
{
    public class BlazVerificationReportsService : IBlazVerificationReportsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IHttpClientHelper _httpClientHelper;

        public BlazVerificationReportsService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<int> GetVenueCountAsync(CancellationToken cancellationToken = default)
        {
            return await _httpClientHelper._httpClient.GetFromJsonAsync<int>("api/VerificationReports/GetVenueCount", cancellationToken);
        }

        public async IAsyncEnumerable<CentersAllocationVerificationResultDto> GetCentersAllocationVerificationReportStreamAsync(CentersAllocationVerificationRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var result in StreamAsync<CentersAllocationVerificationResultDto>("api/VerificationReports/GetCentersAllocationVerificationReport", request, cancellationToken))
            {
                yield return result;
            }
        }

        public async IAsyncEnumerable<VenueAttendanceVerificationResultDto> GetVenueAttendanceVerificationReportStreamAsync(
            VenueAttendanceVerificationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            await foreach (var result in StreamAsync<VenueAttendanceVerificationResultDto>("api/VerificationReports/GetVenueAttendanceVerificationReport", request, cancellationToken))
            {
                yield return result;
            }
        }

        private async IAsyncEnumerable<T> StreamAsync<T>(string endpoint, object request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(request)
            };

            using var response = await _httpClientHelper._httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var result = JsonSerializer.Deserialize<T>(line, JsonOptions);

                if (result is not null)
                {
                    yield return result;
                }
            }
        }
    }
}