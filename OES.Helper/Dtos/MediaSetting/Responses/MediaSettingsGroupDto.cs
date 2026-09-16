namespace OES.Helper.Dtos.MediaSetting.Responses
{
    public class MediaSettingsGroupDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
