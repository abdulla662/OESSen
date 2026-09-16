using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Venue
{
    public class BlazVenueService : IBlazVenueService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<GetPaginatedVenueResponseDto> _blazGetCustomTableData;

        public BlazVenueService(IHttpClientHelper httpClientHelper, IBlazGetCustomTableData<GetPaginatedVenueResponseDto> blazGetCustomTableData)
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<CustomTableData<GetPaginatedVenueResponseDto>> GetAllPaginatedVenuesAsync(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/Venue/GetAllPaginatedVenues");
        }

        public async Task<List<GetVenueResponseDto>> GetAllVenuesAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<GetVenueResponseDto>>("api/Venue/GetAllVenues");

            return (List<GetVenueResponseDto>)response.Data ?? [];
        }

        public async Task<List<VenueLookupDto>> GetVenueLookupAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<VenueLookupDto>>("api/Venue/GetVenueLookup");

            return (List<VenueLookupDto>)response.Data ?? [];
        }

        public Task<ApiResponse> GetVenueById(long id)
        {
            return _httpClientHelper.GetAsync<EditVenueRequestDto>($"api/Venue/GetVenueById?id={id}");
        }

        public async Task<ApiResponse> AddVenueAsync(AddVenueRequestDto addVenueRequestDto)
        {
            var respone = await _httpClientHelper.PostAsync(addVenueRequestDto, "api/Venue/AddVenue");

            return respone;
        }

        public async Task<ApiResponse> AddMultipleVenueAsync(AddMultipleVenuesRequestDto addMultipleVenuesRequestDto)
        {
            var respone = await _httpClientHelper.PostAsync(addMultipleVenuesRequestDto, "api/Venue/AddMultipleVenues");

            return respone;
        }

        public Task<ApiResponse> EditVenueAsync(EditVenueRequestDto editVenueRequestDto)
        {
            return _httpClientHelper.PostAsync(editVenueRequestDto, "api/Venue/EditVenue");
        }

        public async Task<ApiResponse> DeleteVenue(long id)
        {
            return await _httpClientHelper.DeleteAsync($"api/Venue/DeleteVenue?id={id}");
        }

        //public async Task<List<GetOESGroupDto>> GetUserVenueGroupsAsync()
        //{
        //    var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/Venue/GetUserVenueGroupsAsync");

        //    var data = (List<GetOESGroupDto>)response.Data;

        //    return data ?? [];
        //}

        //public async Task<VenueGroupsDto> GetVenueGroupsAsync(long VenueId)
        //{
        //    var response = await _httpClientHelper.GetAsync<VenueGroupsDto>($"api/Venue/GetVenueGroupsAsync?VenueId={VenueId}");

        //    return response.Data as VenueGroupsDto ?? new VenueGroupsDto();
        //}
    }
}