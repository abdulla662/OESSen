using OES.Helper.Dtos.Form;
using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Helper.Dtos.UserPapersDto
{
    public class UserPapersListDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public PaperType Type { get; set; }

        public string TypeDisplay => Type.ToLocalizedString();

        public string SubType { get; set; }

        public string SubTypeDisplay => SubType?.ToLocalizedString<QuestionSelectionType>();

        public PaperCreationStatus PaperCreationStatus { get; set; }

        public string PaperCreationStatusDisplay => PaperCreationStatus.ToLocalizedString();

        public AvailabilityStatus PaperStatus { get; set; }

        public string PaperStatusDisplay => PaperStatus.ToLocalizedString();

        public long OutputFormsCount { get; set; }

        public long ActualFormsCount { get; set; }

        public bool IsOutputFormsCountEqualsActualFormsCount => OutputFormsCount == ActualFormsCount;

        public List<FormStatusCountDto> FormsStatusCounts { get; set; } = [];

        public string FormsStatusCountsSummary => PaperStatusHtmlBuilder.PaperStatusHtmlBuilder.BuildFormsStatusSummary(FormsStatusCounts);
    }
}
