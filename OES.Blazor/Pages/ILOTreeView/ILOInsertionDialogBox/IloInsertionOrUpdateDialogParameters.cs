using OES.Helper.Dtos.OESUserGroups;

namespace OES.Blazor.Pages.ILOTreeView.ILOInsertionDialogBox
{
    public class IloInsertionOrUpdateDialogParameters
    {
        public long Id { get; set; }

        public long? ParentId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; } = "";

        public string Code { get; set; }

        public bool IsActive { get; set; } = true;

        public long OrganizationId { get; set; }

        public List<Guid> GroupsIds { get; set; }

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];
    }
}
