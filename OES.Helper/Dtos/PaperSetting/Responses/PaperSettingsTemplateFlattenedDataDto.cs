using OES.Helper.Enums;

namespace OES.Helper.Dtos.PaperSetting.Responses
{
    public class PaperSettingsTemplateFlattenedDataDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public QuestionsDisplayMode QuestionsSequence { get; set; }

        public QuestionsDisplayMode QuestionsOptionsSequence { get; set; }

        public long MediaRepeatCount { get; set; }

        public long TimeForEndingExam { get; set; }

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

        public string WaterMark { get; set; }

        public bool SupervisorPasswordToStartExam { get; set; }

        public bool SupervisorPasswordToEndExam { get; set; }

        public bool AskForCandidatePasswordAtTheEndOfTest { get; set; }

        public bool RunThisExamOnSecureBrowserOnly { get; set; }

        public bool ReplaceNotAttemptedQuestion { get; set; }

        public bool PassingConcept { get; set; }

        public long MinimumPassingMarks { get; set; }

        public bool NegativeMarking { get; set; }

        public float NegativeMarkPercent { get; set; }

        public bool ShowAnalysis { get; set; }

        public bool ShowPrevious { get; set; }

        public bool ShowNext { get; set; }

        public int TrialsCount { get; set; }

        public int LoginToleranceTime { get; set; }

        // Additional Fields

        public bool IdenticalQuestionPaperToAllCandidate { get; set; }
        public bool IdenticalItemSequenceToAllCandidate { get; set; }
        public bool OptionRandomization { get; set; }
        public long EnableEndTestButtonAfterTimeSpent { get; set; }

        // Exam Interface Controllers

        public bool ShowQuestionPaper { get; set; }
        public bool SkipQuestion { get; set; }
        public bool AnsweredQuestionCanBeChanged { get; set; }
        public bool ShowResetQuestion { get; set; }
        public bool ShowNotePad { get; set; }
        public bool RequireAnswerBeforeProceeding { get; set; }

        // Media Playback Settings

        public int MediaPlayingCount { get; set; }
        public bool AskSupervisorForPlayMediaAgain { get; set; }
        public bool AutoStartEnabled { get; set; }

        // Result Screen Settings

        public long? ResultTemplateId { get; set; }
        public long? CertificateTemplateId { get; set; }
        public bool IsCertificateRequired { get; set; }

        // Exam Instruction Settings

        public long InstructionTemplateId { get; set; }
        public long? OtherInstructionTemplateId { get; set; }
        public long DisclaimerTemplateId { get; set; }
        public long? RuleTemplateId { get; set; }

        // Timer Alert Settings

        public bool ApplyTimerAlert { get; set; }
        public int TimerAlertMinutes { get; set; }
    }
}