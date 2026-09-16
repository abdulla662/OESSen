using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;

namespace OES.Blazor.Services.Implementation.Report
{
    public class BlazAnalyticalReportsService : IBlazAnalyticalReportsService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazAnalyticalReportsService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<List<PaperFormLookupDto>> GetFormsAsync(PaperFormsRequestDto paperFormsRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(paperFormsRequestDto, "api/AnalyticalReports/GetForms");

            if (response?.Data is null)
            {
                return [];
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<PaperFormLookupDto>>(response.Data.ToString()) ?? [];
        }

        public async Task<List<ItemBankLookupDto>> GetItemBanksAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<ItemBankLookupDto>>("api/AnalyticalReports/GetItemBanks");
            return response?.Data as List<ItemBankLookupDto> ?? [];
        }

        public async Task<List<PaperCodeLookupDto>> GetPapersAsync(List<long> scheduleIds)
        {
            var qs = string.Join("&", scheduleIds.Select(id => $"{nameof(scheduleIds)}={id}"));
            var response = await _httpClientHelper.GetAsync<List<PaperCodeLookupDto>>($"api/AnalyticalReports/GetPapers?{qs}");
            return response?.Data as List<PaperCodeLookupDto> ?? [];
        }

        public async Task<List<ScheduleLookupDto>> GetSchedulesAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<ScheduleLookupDto>>("api/AnalyticalReports/GetSchedules");
            return response?.Data as List<ScheduleLookupDto> ?? [];
        }

        public async Task<List<VenueLookupDto>> GetVenuesAsync(List<long> scheduleIds)
        {
            var qs = string.Join("&", scheduleIds.Select(id => $"{nameof(scheduleIds)}={id}"));
            var response = await _httpClientHelper.GetAsync<List<VenueLookupDto>>($"api/AnalyticalReports/GetVenues?{qs}");
            return response?.Data as List<VenueLookupDto> ?? [];
        }

        public async Task<CondensedTestReportDto> GetCondensedTestReportAsync(CondensedTestReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/AnalyticalReports/GetCondensedTestReport");

            if (response?.Data is null)
            {
                return new CondensedTestReportDto();
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<CondensedTestReportDto>(response.Data.ToString()!) ?? new CondensedTestReportDto();
        }

        public async Task<ItemTypeReportDto> GetItemTypeReportAsync(long? itemBankId)
        {
            var url = itemBankId.HasValue
                ? $"api/AnalyticalReports/GetItemTypeReport?{nameof(itemBankId)}={itemBankId}"
                : "api/AnalyticalReports/GetItemTypeReport";

            var response = await _httpClientHelper.GetAsync<ItemTypeReportDto>(url);

            return response?.Data as ItemTypeReportDto ?? new ItemTypeReportDto();
        }

        public async Task<PaperBlueprintReportDto> GetPaperBlueprintReportAsync(PaperBlueprintReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/AnalyticalReports/GetPaperBlueprintReport");

            return Newtonsoft.Json.JsonConvert.DeserializeObject<PaperBlueprintReportDto>(response?.Data.ToString()!) ?? new PaperBlueprintReportDto(
                string.Empty,
                string.Empty,
                [],
                []);
        }

        public async Task<PerformanceSummaryReportDto> GetPerformanceSummaryReportAsync(PerformanceSummaryReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/AnalyticalReports/GetPerformanceSummaryReport");

            if (response?.Data is null)
            {
                return new PerformanceSummaryReportDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    [],
                    [],
                    [],
                    []);
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<PerformanceSummaryReportDto>(response.Data.ToString()!)
                ?? new PerformanceSummaryReportDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    [],
                    [],
                    [],
                    []);
        }

        public async Task<QuestionUsageReportDto> GetQuestionUsageReportAsync(QuestionUsageReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/AnalyticalReports/GetQuestionUsageReport");

            if (response?.Data is null)
            {
                return new QuestionUsageReportDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    []);
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<QuestionUsageReportDto>(response.Data.ToString()!)
                ?? new QuestionUsageReportDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    []);
        }

        public async Task<TestAnalysisReportDto> GetTestAnalysisReportAsync(TestAnalysisReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/AnalyticalReports/GetTestAnalysisReport");

            if (response?.Data is null)
            {
                return new TestAnalysisReportDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    [],
                    new TestAnalysisStatsDto(
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        null,
                        0,
                        null,
                        0,
                        0));
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<TestAnalysisReportDto>(response.Data.ToString()!)
                ?? new TestAnalysisReportDto(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    [],
                    new TestAnalysisStatsDto(
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        null,
                        0,
                        null,
                        0,
                        0));
        }

        public async Task<QuestionBlockActivityReportDto> GetQuestionBlockActivityReportAsync(QuestionBlockActivityReportRequestDto request)
        {
            var response = await _httpClientHelper.PostAsync(request, "api/AnalyticalReports/GetQuestionBlockActivityReport");

            if (response?.Data is null)
            {
                return new QuestionBlockActivityReportDto(null, []);
            }

            return Newtonsoft.Json.JsonConvert.DeserializeObject<QuestionBlockActivityReportDto>(response.Data.ToString()!) ?? new QuestionBlockActivityReportDto(null, []);
        }
    }
}
