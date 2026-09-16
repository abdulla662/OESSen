using OES.Helper.Enums;

namespace OES.Blazor.Models.Results
{
    public class ActivityLogModel
    {
        public int No { get; set; }
        public EventListener Action { get; set; }
        public string LogMessage { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public int QuestionId { get; set; }
        public int QuestionStatus { get; set; }
        public double? DurationInSeconds { get; set; }
        public string ElementType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Pathname { get; set; } = string.Empty;
        public string? IpAddress { get; set; } 
    }
}
