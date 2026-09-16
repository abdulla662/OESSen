using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos;

namespace OES.Helper.Dtos.Question.ComprehensionQuestionDtos
{
    public class ComprehensionQuestionWithSubQuestionsDto
    {
        public SubQuestionDetailsDto ComprehensionBody { get; set; } = new();

        public List<SubQuestionDetailsDto> SubQuestionsList { get; set; } = [];
    }
}
