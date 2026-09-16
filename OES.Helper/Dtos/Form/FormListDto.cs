using OES.Helper.Enums;

namespace OES.Helper.Dtos.Form
{
    public class FormListDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public long PaperId { get; set; }

        public long FormQuestionsCount { get; set; }

        public long UsedQuestion { get; set; }

        public long UnusedQuestion { get; set; }

        public AvailabilityStatus FormStatus { get; set; }

        public string FormStatusDisplay => FormStatus.ToLocalizedString();

        public AvailabilityStatus PaperStatus { get; set; }

        public QuestionSelectionType PaperSubType { get; set; }

        public bool IsSuspendedInAnyVenue { get; set; }
    }
}
