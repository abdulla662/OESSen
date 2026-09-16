
namespace OES.Helper.Dtos.Results
{
    public class PaperFormAttendanceReportDto
    {
        // Paper Form Info
        public long PaperFormId { get; set; }
        public string? ExamSeries { get; set; }

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
        public bool SentToCTR { get; set; }
        public int NumSentToCTR { get; set; }

        // Schedule Info
        public int ScheduleId { get; set; }
        public string? ScheduleName { get; set; }
        public string? ScheduleCode { get; set; }
        public string? VenueCode { get; set; }
        public string? VenueDisplayName { get; set; }

        // Computed Properties
        public decimal AttendanceRate => Allocated > 0
            ? Math.Round((decimal)Attended / Allocated * 100, 2)
            : 0;

        public decimal AbsenceRate => Allocated > 0
            ? Math.Round((decimal)Absent / Allocated * 100, 2)
            : 0;

        public decimal CompletionRate => Attended > 0
            ? Math.Round((decimal)TakenSession / Attended * 100, 2)
            : 0;
    }
}
