namespace OES.Helper.Dtos.ExamServer
{
    public class ScheduleSyncResponseDto
    {
        public long OriginalScheduleId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        // JSON Properties

        public string SecurityConfiguration { get; set; }
        public string Languages { get; set; }
    }
}