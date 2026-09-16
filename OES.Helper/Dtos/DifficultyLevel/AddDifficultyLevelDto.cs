using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.DifficultyLevel
{
    public sealed record AddDifficultyLevelDto
    {
        [Required, MaxLength(250)]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.DeltaTypeIsRequired))]
        public long DeltaTypeId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.FromDeltaIsRequired)), Range(0, 1)]
        public decimal FromDelta { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ToDeltaIsRequired)), Range(0, 1)]
        public decimal ToDelta { get; set; }

        [Required]
        public long DifficultyProfileId { get; set; }

        public long OrganizationId { get; set; }
    }
}
