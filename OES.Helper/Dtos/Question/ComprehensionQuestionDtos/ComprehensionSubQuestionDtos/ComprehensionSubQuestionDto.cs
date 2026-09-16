using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos;

namespace OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos
{
    public class ComprehensionSubQuestionDto
    {
        public SubQuestionMetadataDto SubQuestionMetadataDto { get; set; } = new();

        public SubQuestionDetailsDto SubQuestionDetailsDto { get; set; } = new();
    }
}
