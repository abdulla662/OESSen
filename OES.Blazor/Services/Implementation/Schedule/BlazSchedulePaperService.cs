using Microsoft.JSInterop;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSummary;
using OES.Helper.General;
using SharedHelper.General;

namespace OES.Blazor.Services.Implementation.Schedule
{
    public class BlazSchedulePaperService : IBlazSchedulePaperService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<SchedulePaperPaginationDto> _blazGetCustomTablePapers;
        private readonly IBlazGetCustomTableData<VenueCandidatesCountDto> _blazGetVenueCandidatesCountDto;
        private readonly IJSRuntime _jsRuntime;

        public BlazSchedulePaperService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<SchedulePaperPaginationDto> blazGetCustomTablePapers, IBlazGetCustomTableData<VenueCandidatesCountDto> blazGetVenueCandidatesCountDto, IJSRuntime jsRuntime)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTablePapers = blazGetCustomTablePapers;
            _blazGetVenueCandidatesCountDto = blazGetVenueCandidatesCountDto;
            _jsRuntime = jsRuntime;
        }

        public async Task<CustomTableData<SchedulePaperPaginationDto>> GetAllSchedulePapersByScheduleIdAsync(PaginationSearchModel paginationSearchModel, long scheduleId)
        {
            var date = await _blazGetCustomTablePapers.GetCustomTableData(paginationSearchModel, $"api/SchedulePaper/GetAllSchedulePapersByScheduleId?{nameof(scheduleId)}={scheduleId}");

            return date ?? new CustomTableData<SchedulePaperPaginationDto>([], 0);
        }

        public async Task<GetSchedulePaperResponseDto> GetSchedulePaperByIdAsync(long schedulePaperId)
        {
            var response = await _httpClientHelper.GetAsync<GetSchedulePaperResponseDto>($"api/SchedulePaper/GetSchedulePaperById?{nameof(schedulePaperId)}={schedulePaperId}");

            return (GetSchedulePaperResponseDto)response.Data;
        }

        public async Task<GetScheduleSummaryDto> GetSchedulePaperSummaryAsync(long scheduleId)
        {
            var response = await _httpClientHelper.GetAsync<GetScheduleSummaryDto>($"api/SchedulePaper/GetSchedulePaperSummary?{nameof(scheduleId)}={scheduleId}");

            return (GetScheduleSummaryDto)response.Data;
        }

        public async Task<ApiResponse> AddSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto addSchedulePaperRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(addSchedulePaperRequestDto, "api/SchedulePaper/AddSchedulePaper");

            return response;
        }

        public async Task<ApiResponse> UpdateSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto updateSchedulePaperRequestDto)
        {
            var response = await _httpClientHelper.PutAsync(updateSchedulePaperRequestDto, "api/SchedulePaper/UpdateSchedulePaper");

            return response;
        }

        public async Task<ApiResponse> DeleteSchedulePaperAsync(long schedulePaperId)
        {
            return await _httpClientHelper.DeleteAsync($"api/SchedulePaper/DeleteSchedulePaper?{nameof(schedulePaperId)}={schedulePaperId}");
        }

        public async Task<ApiResponse> GetSchedulePaperAllocation(long schedulePaperId)
        {
            return await _httpClientHelper.GetAsync<List<GetSchedulePaperAllocationResponseDto>>($"api/SchedulePaper/GetSchedulePaperAllocation?{nameof(schedulePaperId)}={schedulePaperId}");
        }

        public async Task<ApiResponse> DeletePaperAllocationAsync(long venueId, long schedulePaperId)
        {
            return await _httpClientHelper.DeleteAsync($"api/SchedulePaper/DeletePaperAllocation?{nameof(venueId)}={venueId}&{nameof(schedulePaperId)}={schedulePaperId}");
        }

        public async Task<List<GetSchedulePaperTimeConfiguration>> GetSchedulePapersAsync(long scheduleMetadataId)
        {
            var response = await _httpClientHelper.GetAsync<List<GetSchedulePaperTimeConfiguration>>($"api/SchedulePaper/GetSchedulePapers?{nameof(scheduleMetadataId)}={scheduleMetadataId}");

            return (List<GetSchedulePaperTimeConfiguration>)response.Data ?? [];
        }

        public async Task<CustomTableData<VenueCandidatesCountDto>> GetVenuesWithCandidatesCountByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            var response = await _blazGetVenueCandidatesCountDto.GetCustomTableData(paginationSearchModel, $"api/SchedulePaper/GetVenuesWithCandidatesCount?{nameof(paperId)}={paperId}");

            return response;
        }

        public async Task<List<string>> GetSchedulePaperVenueCodesAsync(long schedulePaperId)
        {
            var response = await _httpClientHelper.GetAsync<List<VenueResponseDto>>($"api/SchedulePaper/GetSchedulePaperVenues?{nameof(schedulePaperId)}={schedulePaperId}");

            var slectedVenueCodes = (response.Data as List<VenueResponseDto>)?.Select(v => v.VenueCode)?.ToList() ?? [];

            return slectedVenueCodes;
        }

        public async Task ExportCandidatesToExcelAsync(long schedulePaperId)
        {
            var url = $"{CentralizedUrlHelper.OesApiBaseUrl}api/SchedulePaper/ExportCandidatesToExcel?{nameof(schedulePaperId)}={schedulePaperId}";

            await _jsRuntime.InvokeVoidAsync(MiscConstants.DownloadFileUsingFetch, url, MiscConstants.CandidatesFileName, null);
        }

        public async Task<ApiResponse> GetVenueBatchesAsync(long venueId, long paperId)
        {
            var response = await _httpClientHelper.GetAsync<List<VenueBatchDetailsDto>>($"api/SchedulePaper/GetVenueBatches?{nameof(venueId)}={venueId}&{nameof(paperId)}={paperId}");

            return response;
        }

        public async Task<SchedulePaperCandidateDetailsDto> GetSchedulePaperCandidateByRegistrationNumberAsync(long registrationNumber)
        {
            var response = await _httpClientHelper.GetAsync<SchedulePaperCandidateDetailsDto>($"api/SchedulePaper/GetSchedulePaperCandidateByRegistrationNumber?{nameof(registrationNumber)}={registrationNumber}");

            return response.Data as SchedulePaperCandidateDetailsDto;
        }

        public async Task<ApiResponse> UpdateSchedulePaperCandidateAsync(UpdateSchedulePaperCandidateRequestDto request)
        {
            return await _httpClientHelper.PutAsync(request, "api/SchedulePaper/UpdateSchedulePaperCandidate");
        }
    }
}