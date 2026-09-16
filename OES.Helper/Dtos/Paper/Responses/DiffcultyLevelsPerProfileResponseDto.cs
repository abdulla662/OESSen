using OES.Helper.Dtos.Paper.Requests;

namespace OES.Helper.Dtos.Paper.Responses
{
    public class DiffcultyLevelsPerProfileResponseDto
    {
        public List<GetListedDifficultyLevelResponseDto> DifficultyLevels { get; set; } = [];

        public List<QuestionTypeCountViewRequestDto> count { get; set; } = [];

        public long QuestionCount { get; set; }
    }
}
