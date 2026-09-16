using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.OrganizationStructure.Requests
{
    public sealed record AddOrUpdateOrganizationStructureNodeRequestDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NodeNameIsRequired))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NodeDescriptionIsRequired))]
        public string Description { get; set; }

        public long? ParentId { get; set; }

        public bool IsLeaf { get; set; }


        public AddOrUpdateOrganizationStructureNodeRequestDto() { }

        public AddOrUpdateOrganizationStructureNodeRequestDto(long? parentId)
        {
            ParentId = parentId;
        }

        [JsonConstructor]
        public AddOrUpdateOrganizationStructureNodeRequestDto(long id,
                                                              string name,
                                                              string description,
                                                              long? parentId,
                                                              bool isLeaf)
        {
            Id = id;
            Name = name;
            Description = description;
            ParentId = parentId;
            IsLeaf = isLeaf;
        }
    }
}
