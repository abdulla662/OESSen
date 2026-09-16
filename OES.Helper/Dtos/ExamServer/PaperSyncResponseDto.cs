using OES.Helper.Enums;

namespace OES.Helper.Dtos.ExamServer
{
    public class PaperSyncResponseDto
    {
        public long OriginalSchedulePaperId { get; set; }
        public long OriginalPaperId { get; set; }
        public long ScheduleId { get; set; }
        public string Type { get; set; }
        public string AdaptiveSubtype { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public string LanguageDirection { get; set; }
        public string PaperDescription { get; set; }
        public string Code { get; set; }
        public AvailabilityStatus PaperStatus { get; set; }
        public string Abbreviation { get; set; }
        public float Duration { get; set; }
        public int QuestionsCount { get; set; }
        public bool AllowInstantResult { get; set; }
        public bool UsesExcelQuestionsImport { get; set; }
        public long TotalMarks { get; set; }
        public string QuestionContentType { get; set; }
        public string SchedulePaperDescription { get; set; }
        public string DPathCalculationMode { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        // JSON Properties

        public string PaperSettings { get; set; }
        public string Subjects { get; set; }
        public string MarkingScheme { get; set; }
        public string? TransitionProfile { get; set; }
        public string? DifficultyProfile { get; set; }
        public string? QuestionCategories { get; set; }
        public string? AdaptiveCategoryExecutionOrder { get; set; }
    }
}