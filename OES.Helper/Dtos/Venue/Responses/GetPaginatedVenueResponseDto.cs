namespace OES.Helper.Dtos.Venue.Responses
{
    public class GetPaginatedVenueResponseDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public string Code { get; set; }

        public string PinCode { get; set; }

        public string TCIds { get; set; }

        public int TCIdsCount => string.IsNullOrWhiteSpace(TCIds) ? 0 : TCIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries).Length;

        public bool IsPBT { get; set; }
    }
}
