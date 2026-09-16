using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.QuestionCategory
{
    public class QuestionCategoryDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameIsRequired))]
        public string Name { get; set; }

        public string? FinalScoreName { get; set; }

        public decimal? StandardDeviation { get; set; }

        public decimal? StandardError1 { get; set; }

        public decimal? StandardError2 { get; set; }

        public decimal? BaseValue1 { get; set; }

        public decimal? BaseValue2 { get; set; }
    }
}