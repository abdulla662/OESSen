using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class VenueController : OESBaseController, IVenueService
    {
        private readonly IVenueService _venueService;

        public VenueController(IVenueService venueService)
        {
            _venueService = venueService;
        }

        [HttpPost("GetAllPaginatedVenues")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllPaginatedVenuesAsync([FromBody] PaginationSearchModel pagination)
        {
            return await _venueService.GetAllPaginatedVenuesAsync(pagination);
        }

        [HttpGet("GetAllVenues")]
        [OESFilter(Authorize = true)]
        public IApiResponse GetAllVenues()
        {
            return _venueService.GetAllVenues();
        }

        [HttpGet("GetVenueLookup")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetVenueLookupAsync()
        {
            return await _venueService.GetVenueLookupAsync();
        }

        [HttpGet("GetVenueById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetVenueById(long id)
        {
            return await _venueService.GetVenueById(id);
        }

        [HttpPost("AddVenue")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddVenueAsync(AddVenueRequestDto venueDto, CancellationToken cancellationToken = default)
        {
            return await _venueService.AddVenueAsync(venueDto);
        }

        [HttpPost("AddMultipleVenues")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddMultipleVenuesAsync(AddMultipleVenuesRequestDto addMultipleVenuesRequestDto, CancellationToken cancellationToken = default)
        {
            return await _venueService.AddMultipleVenuesAsync(addMultipleVenuesRequestDto);
        }

        [HttpPost("EditVenue")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> EditVenueAsync(EditVenueRequestDto editVenueRequestDto)
        {
            return await _venueService.EditVenueAsync(editVenueRequestDto);
        }

        [HttpDelete("DeleteVenue")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeleteVenue(long id)
        {
            return await _venueService.DeleteVenue(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserVenueGroupsAsync")]
        //public async Task<IApiResponse> GetUserVenueGroupsAsync()
        //{
        //    return await _venueService.GetUserVenueGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetVenueGroupsAsync")]
        //public async Task<IApiResponse> GetVenueGroupsAsync(long VenueId)
        //{
        //    return await _venueService.GetVenueGroupsAsync(VenueId);
        //}
    }
}