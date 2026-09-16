using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.Paper.Responses;

namespace OES.Helper.Dtos.Paper.Requests
{
    public class ManualSectioningRequestDto
    {
        public List<ManuallySelectedQuestionsResponseDto> Questions { get; set; } = [];
        public FormMetadataDto FormsMetadata { get; set; } = new();
        public Dictionary<string, List<SectionRequestDto>> FormSections { get; set; } = [];
        public Dictionary<string, List<long>> FormQuestionMap { get; set; } = [];
    }
}