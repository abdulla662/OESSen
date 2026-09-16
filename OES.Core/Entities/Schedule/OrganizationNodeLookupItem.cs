using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class OrganizationNodeLookupItem : BaseEntity<long>
    {
        // Properties

        public string Name { get; private set; }

        public bool IsRoot { get; private set; }

        public long? ParentId { get; private set; }

        public long OrganizationStructureNodeId { get; private set; }


        // Domain Methods

        public OrganizationNodeLookupItem() { }

        private OrganizationNodeLookupItem(string name,
                                           bool isRoot,
                                           long? parentId,
                                           long organizationStructureNodeId)
        {
            Name = name;
            IsRoot = isRoot;
            ParentId = parentId;
            OrganizationStructureNodeId = organizationStructureNodeId;
        }

        public static OrganizationNodeLookupItem Create(string name,
                                                        bool isRoot,
                                                        long? parentId,
                                                        long organizationStructureNodeId)
        {
            return new OrganizationNodeLookupItem(name, isRoot, parentId, organizationStructureNodeId);
        }

        public void Update(string name, long? parentId)
        {
            Name = name;
            ParentId = parentId;
        }

        public void SoftDelete()
        {
            IsDeleted = true;
        }


        // Navigational Properties

        [ForeignKey(nameof(ParentId))]
        public OrganizationNodeLookupItem? ParentLookupItem { get; private set; }

        public ICollection<OrganizationNodeLookupItem> ChildLookupItems { get; private set; } = [];

        [ForeignKey(nameof(OrganizationStructureNodeId))]
        public OrganizationStructure OrganizationStructureNode { get; private set; }

        public ICollection<CandidateOrganizationNodeLookupItem> Candidates { get; private set; } = [];
    }
}
