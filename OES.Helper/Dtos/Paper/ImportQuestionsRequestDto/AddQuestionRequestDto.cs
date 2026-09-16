using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Paper.ImportQuestionsRequestDto
{
    public class AddQuestionRequestDto
    {
        [Required(ErrorMessageResourceName = nameof(Resource.QuestionCodeRequired), ErrorMessageResourceType = typeof(Resource))]
        public string QuestionCode { get; set; }

        [Required(ErrorMessageResourceName = nameof(Resource.SectionNameRequired), ErrorMessageResourceType = typeof(Resource))]
        public string SectionName { get; set; }
    }
}
