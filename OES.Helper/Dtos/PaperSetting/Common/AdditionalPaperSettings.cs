namespace OES.Helper.Dtos.PaperSetting.Common
{
    public class AdditionalPaperSettings
    {
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

        public int MediaPlayingCount { get; set; } = 1;
        public bool AskSupervisorForPlayMediaAgain { get; set; }
        public bool AutoStartEnabled { get; set; }

        // Result Screen Settings

        public long? ResultTemplateId { get; set; }
        public long? CertificateTemplateId { get; set; }
        public bool IsCertificateRequired { get; set; } = true;

        // Exam Instruction Settings

        public long InstructionTemplateId { get; set; }
        public long? OtherInstructionTemplateId { get; set; }
        public long DisclaimerTemplateId { get; set; }
        public long? RuleTemplateId { get; set; }

        // Timer Alert Settings
        public bool ApplyTimerAlert { get; set; }
        public int TimerAlertMinutes { get; set; } = 1;
    }
}