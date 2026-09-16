using OES.Helper.Dtos.Question;

namespace OES.Helper.Dtos.Section
{
    public class SectionWithFormQuestionsDto
    {
        public long SectionId { get; set; }

        public string SectionName { get; set; }

        public string SectioningIdentifier { get; set; }

        public List<QuestionsInSectionDto> Questions { get; set; } = [];
    }
}
