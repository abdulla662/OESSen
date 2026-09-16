namespace OES.Blazor.Models.RuleMatrix
{
    public class ResourceInstanceItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? SubLabel { get; set; }
        public string? Signature { get; set; }
        public bool IsLeaf { get; set; }
        public bool IsSelected { get; set; }
        public bool IsExpanded { get; set; }
        public bool ChildrenLoaded { get; set; }
        public bool IsLoadingChildren { get; set; }
        public int Depth { get; set; } = 0;
        public long? ParentId { get; set; }
        public List<ResourceInstanceItem> Children { get; set; } = [];
    }
}