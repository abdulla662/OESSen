using OES.Core.Entities.Paper;
using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class SchedulePaperSettings : BaseEntity<long>
    {
        [Required]
        public QuestionsDisplayMode QuestionsSequence { get; set; }

        [Required]
        public QuestionsDisplayMode QuestionsOptionsSequence { get; set; }

        [Required]
        public bool ShowProfile { get; set; }

        [Required]
        public bool ShowInstructions { get; set; }

        [Required]
        public bool DisablePalletNavigation { get; set; }

        [Required]
        public bool AnswerOptionCanBeChangedAfterAttempt { get; set; }

        [Required]
        public bool MarkForReviewQuestion { get; set; }

        [Required]
        public bool ShowResetAnswerOption { get; set; }

        [Required]
        public bool AutoSaveOnTimeUp { get; set; }

        [Required]
        public bool AutoSaveOnClosing { get; set; }

        [Required]
        public bool ShowScientificCalculator { get; set; }

        [Required]
        public bool ShowWatermark { get; set; }

        public string WaterMark { get; set; }

        [Required]
        public bool SupervisorPasswordToStartExam { get; set; }

        [Required]
        public bool SupervisorPasswordToEndExam { get; set; }

        [Required]
        public bool AskForCandidatePasswordAtTheEndOfTest { get; set; }

        [Required]
        public bool ReplaceNotAttemptedQuestion { get; set; }

        public bool PassingConcept { get; set; }

        [Required]
        public long MinimumPassingMarks { get; set; }

        public bool NegativeMarking { get; set; }

        public float NegativeMarkPercent { get; set; }

        public bool ShowAnalysis { get; set; }

        public bool ShowPrevious { get; set; }

        public bool ShowNext { get; set; }

        public int TrialsCount { get; set; } = 1;

        public int LoginToleranceTime { get; set; } = 1;

        public bool RunThisExamOnSecureBrowserOnly { get; set; }

        // Additional Fields

        public bool IdenticalQuestionPaperToAllCandidate { get; set; }
        public bool IdenticalItemSequenceToAllCandidate { get; set; }
        public bool OptionRandomization { get; set; }
        public long EnableEndTestButtonAfterTimeSpent { get; set; }
        public long SchedulePaperId { get; set; }

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

        public bool IsCertificateRequired { get; set; }
        public long? ResultTemplateId { get; set; }
        public long? CertificateTemplateId { get; set; }

        // Exam Instruction Settings

        public long InstructionTemplateId { get; set; }
        public long? OtherInstructionTemplateId { get; set; }
        public long DisclaimerTemplateId { get; set; }
        public long? RuleTemplateId { get; set; }

        // Timer Alert Settings

        public bool ApplyTimerAlert { get; set; }

        public int TimerAlertMinutes { get; set; }

        // Navigational Properties

        [ForeignKey(nameof(SchedulePaperId))]
        public SchedulePaper SchedulePaper { get; set; }

        [ForeignKey(nameof(InstructionTemplateId))]
        public Template InstructionTemplate { get; set; }

        [ForeignKey(nameof(OtherInstructionTemplateId))]
        public Template? OtherInstructionTemplate { get; set; }

        [ForeignKey(nameof(DisclaimerTemplateId))]
        public Template DisclaimerTemplate { get; set; }

        [ForeignKey(nameof(CertificateTemplateId))]
        public Template? CertificateTemplate { get; set; }

        [ForeignKey(nameof(ResultTemplateId))]
        public Template? ResultTemplate { get; set; }

        [ForeignKey(nameof(RuleTemplateId))]
        public Template? RuleTemplate { get; set; }
    }
}