
namespace OES.Helper.Dtos.Paper.Responses
{
    public class ManualQuestionsResultDto
    {
        public List<ManualQuestionsPaginationResponseDto> Questions { get; set; } = [];

        public int TotalUsedCount { get; set; }
    }
}
