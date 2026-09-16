using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.OrganizationStructure.Requests
{
    public sealed record AddOrUpdateOrganizationStructureRootRequestDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootNameIsRequired))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.RootDescriptionIsRequired))]
        public string Description { get; set; }

        public AddOrUpdateOrganizationStructureRootRequestDto() { }

        [JsonConstructor]
        public AddOrUpdateOrganizationStructureRootRequestDto(
            long id,
            string name,
            string description
        )
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}