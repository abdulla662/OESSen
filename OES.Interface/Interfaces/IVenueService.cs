using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IVenueService
    {
        Task<IApiResponse> GetAllPaginatedVenuesAsync(PaginationSearchModel pagination);

        IApiResponse GetAllVenues();

        Task<IApiResponse> GetVenueLookupAsync();

        Task<IApiResponse> GetVenueById(long id);

        Task<IApiResponse> AddVenueAsync(AddVenueRequestDto venueDto, CancellationToken cancellationToken = default);

        Task<IApiResponse> AddMultipleVenuesAsync(AddMultipleVenuesRequestDto addMultipleVenuesRequestDto, CancellationToken cancellationToken = default);

        Task<IApiResponse> EditVenueAsync(EditVenueRequestDto editVenueRequestDto);

        Task<IApiResponse> DeleteVenue(long id);

        //Task<IApiResponse> GetUserVenueGroupsAsync();

        //Task<IApiResponse> GetVenueGroupsAsync(long VenueId);
    }
}
