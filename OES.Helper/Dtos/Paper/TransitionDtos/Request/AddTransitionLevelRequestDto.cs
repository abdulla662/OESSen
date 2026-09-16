using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Paper.TransitionDtos.Request
{
    public class AddTransitionLevelRequestDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameIsRequired))]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.TransationProfileIsRequired))]
        public long TransitionProfileId { get; set; }

        public decimal LowerDScore { get; set; }

        public decimal UpperDScore { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.DifficultyLevelIsRequired))]
        [Range(1, long.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.DifficultyLevelIsRequired))]
        public long DifficultyLevelId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.QuestionCategoryIsRequired))]
        [Range(1, long.MaxValue, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.QuestionCategoryIsRequired))]
        public long QuestionCategoryId { get; set; }

    }
}
