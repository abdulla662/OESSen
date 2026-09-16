using Newtonsoft.Json;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports;
using OES.Helper.Dtos.Reports.Analytical;

namespace OES.Blazor.Services.Implementation.Report
{
    public class BlazResultsReportsService : IBlazResultsReportsService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazResultsReportsService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<RawScoreResultsPagedDto> GetRawScoreResultsReportAsync(RawScoreResultsReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/ResultsReports/GetRawScoreResultsReport");

            if (response?.Data is null)
            {
                return new RawScoreResultsPagedDto(0,
                    [],
                    string.Empty,
                    []);
            }

            return JsonConvert.DeserializeObject<RawScoreResultsPagedDto>(response.Data.ToString()!)
                ?? new RawScoreResultsPagedDto(0,
                    [],
                    string.Empty,
                    []);
        }

        public async Task<List<PaperLookupDto>> GetItemAnalysisPapersAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<PaperLookupDto>>("api/ResultsReports/GetItemAnalysisPapers");
            return response?.Data as List<PaperLookupDto> ?? [];
        }

        public async Task<List<FormLookupDto>> GetItemAnalysisFormsAsync(string paperCode)
        {
            var response = await _httpClientHelper.GetAsync<List<FormLookupDto>>($"api/ResultsReports/GetItemAnalysisForms?{nameof(paperCode)}={Uri.EscapeDataString(paperCode)}");
            return response?.Data as List<FormLookupDto> ?? [];
        }

        public async Task<ScoreDistributionReportDto> GetScoreDistributionReportAsync(ScoreDistributionReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/ResultsReports/GetScoreDistributionReport");

            if (response?.Data is null)
            {
                return new(
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    [],
                    [],
                    []);
            }

            return JsonConvert.DeserializeObject<ScoreDistributionReportDto>(response.Data.ToString()!) ?? new(
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                [],
                [],
                []);
        }

        public async Task<ExportFileDto> GetResponseMatrixReportAsync(ResponseMatrixReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/ResultsReports/GetResponseMatrixReport");

            if (response?.Data is null)
            {
                return new(string.Empty, string.Empty, []);
            }

            return JsonConvert.DeserializeObject<ExportFileDto>(response.Data.ToString()!) ?? new(string.Empty, string.Empty, []);
        }

        public async Task<ExportFileDto> GetItemAnalysisReportAsync(ItemAnalysisReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/ResultsReports/GetItemAnalysisReport");

            if (response?.Data is null)
            {
                return new ExportFileDto(
                    string.Empty,
                    string.Empty,
                    []);
            }

            return JsonConvert.DeserializeObject<ExportFileDto>(response.Data.ToString()!)
                ?? new ExportFileDto(
                    string.Empty,
                    string.Empty,
                    []);
        }
    }
}
