using OES.Helper.Enums;

namespace OES.Helper.Dtos.TrackingLog
{
    public class TrackingLogEntryDto
    {
        public EventListener Action { get; set; }
        public string? LogMessage { get; set; }
        public DateTime Time { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        public int QuestionId { get; set; }
        public int QuestionStatus { get; set; }
        public double? DurationInSeconds { get; set; }
        public string? ElementType { get; set; }
        public string? Url { get; set; }
        public string? Pathname { get; set; }
        public string? IpAddress { get; set; }
    }
}
