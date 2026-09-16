namespace OES.Helper.Dtos.Venue.Requests
{
    public sealed record AddMultipleVenuesRequestDto(IEnumerable<AddVenueRequestDto> AddVenueRequestDtos);
}
