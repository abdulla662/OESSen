using OES.Helper.Dtos.Question.QuestionMetadataDtos;

namespace OES.Helper.Dtos.Question
{
    public class GetQuestionTemplateResponseDto
    {
        public QuestionMetadataRetrievalDto QuestionMetadata { get; set; } = new();

        public AIQuestionTemplateCreationDto AIQuestionTemplate { get; set; } = new();
    }
}
