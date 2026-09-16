namespace OES.Core.Entities.Schedule
{
    public class ScheduleTemplate : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Data { get; set; }
    }
}
