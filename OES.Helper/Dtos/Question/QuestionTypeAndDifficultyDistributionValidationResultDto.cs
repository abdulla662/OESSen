using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.Question
{
    public class QuestionTypeAndDifficultyDistributionValidationResultDto
    {
        public bool IsValid { get; set; }

        public QuestionTypeAndDifficultyDistributionValidationResultDto() { }

        [JsonConstructor]
        public QuestionTypeAndDifficultyDistributionValidationResultDto(bool isValid)
        {
            IsValid = isValid;
        }
    }
}
