namespace OES.Helper.Dtos.Schedule.Responses
{
    public class ScheduleGroupsDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
