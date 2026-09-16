namespace OES.Helper.Dtos.ItemBank
{
    public class ItemBankGroupsDto
    {
        public List<Guid> GroupsIds { get; set; } = [];

        public Guid? OwnerGroupId { get; set; }
    }
}
