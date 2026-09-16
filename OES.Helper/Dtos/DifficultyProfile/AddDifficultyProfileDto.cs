
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.DifficultyProfile
{
    public class AddDifficultyProfileDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = "NameIsRequired")]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = "DescriptionIsRequired")]
        public string Description { get; set; }
    }
}

