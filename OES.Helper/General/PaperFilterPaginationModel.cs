using OES.Helper.Enums;
using SharedHelper.Enums;

namespace OES.Helper.General
{
    public class PaperFilterPaginationModel
    {
        public PaperType? SelectedPaperType { get; set; }

        public QuestionSelectionType? QuestionSelectionType { get; set; }

        public PaperCreationStatus? SelectedPaperCreationStatus { get; set; }

        public bool? IsComplete { get; set; }
    }
}
