using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.DifficultyLevel
{
    public sealed record UpdateDifficultyLevelDto
    {
        public long Id { get; set; }

        [Required, MaxLength(250)]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.FromDeltaIsRequired)), Range(0, 1)]
        public decimal FromDelta { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ToDeltaIsRequired)), Range(0, 1)]
        public decimal ToDelta { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.Deltatypeidisrequired))]
        public long DeltaTypeId { get; set; }

        [Required]
        public long? DifficultyProfileId { get; set; }
    }
}
