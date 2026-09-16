using OES.Helper.Dtos.Paper.Responses;

namespace OES.Helper.Dtos.Paper.Requests
{
    public class AutoSectioningRequestDto
    {
        public List<AutoSelectedQuestionsResponseDto> Questions { get; set; }
        public List<SectionRequestDto> Sections { get; set; }
    }
}
