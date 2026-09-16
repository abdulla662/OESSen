using OES.Helper.Dtos.PaperSetting.Common;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.PaperSetting.Responses
{
    public class GetPaperSettingsResponseDto
    {
        public long Id { get; set; }

        public QuestionsDisplayMode QuestionsSequence { get; set; }

        public QuestionsDisplayMode QuestionsOptionsSequence { get; set; }

        public bool ShowProfile { get; set; }

        public bool ShowInstructions { get; set; }

        public bool DisablePalletNavigation { get; set; }

        public bool AnswerOptionCanBeChangedAfterAttempt { get; set; }

        public bool MarkForReviewQuestion { get; set; }

        public bool ShowResetAnswerOption { get; set; }

        public bool AutoSaveOnTimeUp { get; set; }

        public bool AutoSaveOnClosing { get; set; }

        public bool ShowScientificCalculator { get; set; }

        public bool ShowWatermark { get; set; }

        public string WaterMark { get; set; } = "Watermark";

        public bool SupervisorPasswordToStartExam { get; set; }

        public bool SupervisorPasswordToEndExam { get; set; }

        public bool AskForCandidatePasswordAtTheEndOfTest { get; set; }

        public bool ReplaceNotAttemptedQuestion { get; set; }

        public bool PassingConcept { get; set; }

        public long MinimumPassingMarks { get; set; } = 1;

        public bool NegativeMarking { get; set; }

        public float NegativeMarkPercent { get; set; } = 1;

        public bool ShowAnalysis { get; set; }

        public bool ShowPrevious { get; set; }

        public bool ShowNext { get; set; }

        public int TrialsCount { get; set; } = 1;

        public int LoginToleranceTime { get; set; } = 1;

        public bool RunThisExamOnSecureBrowserOnly { get; set; }

        public AdditionalPaperSettings AdditionalPaperSettings { get; set; } = new();
    }
}
