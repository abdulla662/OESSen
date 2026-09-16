using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class OrganizationStructure : BaseEntity<long>
    {
        // Properties

        public string Name { get; private set; }

        public string Description { get; private set; }

        public string OrganizationStructureSignature { get; private set; }

        public long? ParentId { get; private set; } = null!;

        public bool IsLeaf { get; private set; }


        // Domain Methods

        public OrganizationStructure() { }

        private OrganizationStructure(long? parentId,
                                      string name,
                                      string description,
                                      string organizationStructureSignature,
                                      bool isLeaf)
        {
            ParentId = parentId;
            Name = name;
            Description = description;
            OrganizationStructureSignature = organizationStructureSignature;
            IsLeaf = isLeaf;
        }

        public static OrganizationStructure Create(long? parentId,
                                                   string name,
                                                   string description,
                                                   string organizationStructureSignature,
                                                   bool isLeaf)
        {
            return new OrganizationStructure(parentId, name, description, organizationStructureSignature, isLeaf);
        }

        public void Update(string name, string description, bool isLeaf)
        {
            Name = name;
            Description = description;
            IsLeaf = isLeaf;
        }

        public void SoftDelete()
        {
            IsDeleted = true;
        }


        // Navigational Properties

        [ForeignKey(nameof(ParentId))]
        public OrganizationStructure? ParentOrganizationStructureNode { get; private set; }

        public ICollection<OrganizationStructure> ChildNodes { get; private set; } = [];

        public ICollection<OrganizationNodeLookupItem> OrganizationNodeLookupItems { get; private set; } = [];
    }
}
