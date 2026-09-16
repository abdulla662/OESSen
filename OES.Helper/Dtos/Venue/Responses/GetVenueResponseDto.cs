namespace OES.Helper.Dtos.Venue.Responses
{
    public sealed record GetVenueResponseDto(
        long Id,
        string Name,
        string Code,
        string TCIds,
        string Address,
        string GeoLocation,
        string PinCode,
        string VenueEmail,
        string Mobile,
        string CoordinatorFullName,
        string CoordinatorEmail,
        string CoordinatorMobile,
        string IPAddress,
        string Url
    );
}
