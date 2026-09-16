namespace OES.Core.Entities.Views.Results
{
    public class PaperFormAttendanceReportView
    {
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public string? PaperFormName { get; set; }
        public string? ExamSeries { get; set; }
        public DateOnly? ExamDate { get; set; }

        // Attendance Stats
        public int Allocated { get; set; }
        public int Attended { get; set; }
        public int Absent { get; set; }
        public int TakenSession { get; set; }
        public int UnfinishedSession { get; set; }
        public string Reviewed { get; set; }

        // Performance Stats
        public decimal? AvgCompletionPercentage { get; set; }
        public double? AvgTimeSpentMinutes { get; set; }

        // Paper Info
        public string? PaperName { get; set; }
        public string? PaperCode { get; set; }
        public float? PaperDuration { get; set; }
        public DateTime? PaperStartDate { get; set; }
        public DateTime? PaperEndDate { get; set; }

        // Schedule Info
        public int ScheduleId { get; set; }
        public string? ScheduleName { get; set; }
        public string? ScheduleCode { get; set; }
        public string? VenueCode { get; set; }
        public string? VenueDisplayName { get; set; }

        // CTR Stats
        public int NumSentToCTR { get; set; }
        public bool SentToCTR { get; set; }
    }
}