using SharedHelper.Enums;

namespace OES.Helper.General
{
    public class BlockFilterPaginationModel
    {
        public long _SelectedDifficultyLevel { get; set; }

        public long _SelectedBlockType { get; set; }

        public AdaptivePaperSubtype _AdaptiveSubtype { get; set; }

        public long _SelectedProfile { get; set; }

        public long _SelectedLanguageId { get; set; }

        public bool _IsStepPlus { get; set; }
    }
}
