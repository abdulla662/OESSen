namespace OES.Helper.Dtos.Schedule.Responses
{
    public class GetSchedulePaperTimeConfiguration
    {
        public long Id { get; set; }

        public long PaperId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }
    }
}
