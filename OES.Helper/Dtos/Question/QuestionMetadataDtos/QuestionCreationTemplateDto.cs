namespace OES.Helper.Dtos.Question.QuestionMetadataDtos
{
    public class QuestionCreationTemplateDto : QuestionMetadataAdditionOrUpdateDto
    {
        public string Name { get; set; }

        public bool IsFromQuestionAI { get; set; } = false;
    }
}
