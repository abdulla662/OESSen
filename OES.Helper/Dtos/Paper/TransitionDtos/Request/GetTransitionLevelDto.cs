using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Paper.TransitionDtos.Request
{
    public class GetTransitionLevelDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameisRequierd))]
        public string Name { get; set; }

        public decimal LowerDScore { get; set; }

        public decimal UpperDScore { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.DifficultyLevelIsRequired))]
        public long DifficultyLevelId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.QuestionCategoryIsRequired))]
        public long QuestionCategoryId { get; set; }

        public string DifficultyLevelName { get; set; }

        public string QuestionCategoryName { get; set; }

        public long TransitionProfileId { get; set; }

        public string TransitionProfileName { get; set; }

        public long DifficultyProfileId { get; set; }

        public string DifficultyProfileName { get; set; }

        public string TransitionProfileDescription { get; set; }

        public string LowerDScoreFormatted => LowerDScore.ToString("F2");

        public string UpperDScoreFormatted => UpperDScore.ToString("F2");

        public decimal LowerDScoreRounded => Math.Round(LowerDScore, 2);

        public decimal UpperDScoreRounded => Math.Round(UpperDScore, 2);
    }
}
