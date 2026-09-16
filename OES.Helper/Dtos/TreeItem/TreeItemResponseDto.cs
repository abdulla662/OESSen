namespace OES.Helper.Dtos.TreeItem
{
    public class TreeItemResponseDto
    {
        public long Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public string Description { get; set; }

        public string Code { get; set; }

        public string Signature { get; set; }

        public long? ParentId { get; set; }

        public float Hours { get; set; }

        public bool IsExpanded { get; set; }

        public bool IsActive { get; set; }

        public long LevelId { get; set; }

        public List<TreeItemResponseDto> Children { get; set; } = [];

        public bool IsChildrenLoaded { get; set; }

        public bool IsLeaf { get; set; }

        public long OrganizationId { get; set; }

        public List<Guid> GroupsIds { get; set; }

        public bool Unscored { get; set; }
    }
}
