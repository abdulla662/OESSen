using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Venue
{
    public interface IBlazVenueService
    {
        Task<CustomTableData<GetPaginatedVenueResponseDto>> GetAllPaginatedVenuesAsync(PaginationSearchModel pagination);

        Task<List<GetVenueResponseDto>> GetAllVenuesAsync();

        Task<List<VenueLookupDto>> GetVenueLookupAsync();

        Task<ApiResponse> GetVenueById(long id);

        Task<ApiResponse> AddVenueAsync(AddVenueRequestDto addVenueRequestDto);

        Task<ApiResponse> AddMultipleVenueAsync(AddMultipleVenuesRequestDto addMultipleVenuesRequestDto);

        Task<ApiResponse> EditVenueAsync(EditVenueRequestDto editVenueRequestDto);

        Task<ApiResponse> DeleteVenue(long id);

        //Task<List<GetOESGroupDto>> GetUserVenueGroupsAsync();

        //Task<VenueGroupsDto> GetVenueGroupsAsync(long VenueId);
    }
}