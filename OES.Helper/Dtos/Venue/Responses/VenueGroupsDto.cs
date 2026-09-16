namespace OES.Helper.Dtos.Venue.Responses
{
    public class VenueGroupsDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
