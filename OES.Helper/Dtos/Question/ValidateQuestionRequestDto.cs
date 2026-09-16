using OES.Helper.Dtos.SectionDistributionDto.Common;

namespace OES.Helper.Dtos.Question
{
    public class ValidateQuestionRequestDto
    {
        public long PaperId { get; set; }

        public List<SectionWithDistributionsDto> Sections { get; set; }
    }
}
