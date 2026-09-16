using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.OrganizationStructure.Requests
{
    public sealed record AddOrUpdateOrganizationNodeLookupItemRequestDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.LookupItemNameIsRequired))]
        public string Name { get; set; }

        public long OrganizationStructureNodeId { get; set; }

        public long? ParentLookupItemId { get; set; }


        public AddOrUpdateOrganizationNodeLookupItemRequestDto() { }


        public AddOrUpdateOrganizationNodeLookupItemRequestDto(long organizationStructureNodeId)
        {
            OrganizationStructureNodeId = organizationStructureNodeId;
        }
    }
}
