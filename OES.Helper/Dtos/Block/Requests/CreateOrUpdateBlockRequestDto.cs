using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Block.Requests
{
    public class CreateOrUpdateBlockRequestDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameIsRequired))]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.CodeIsRequired)), MaxLength(50)]
        public string Code { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired))]
        public long BlockTypeId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired))]
        public long DeltaTypeId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired))]
        public long DifficultyLevelId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired))]
        public long LanguageId { get; set; }

        public List<long> QuestionsIds { get; set; } = [];

        public List<Guid> OESGroupIds { get; set; } = [];

        public bool ConsiderDifficultyLevel { get; set; } = true;
    }
}
