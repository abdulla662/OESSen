namespace OES.Helper.Dtos.Schedule.Requestes;

public class AddOrUpdateSchedulePaperRequestDto
{
    public long SchedulePaperId { get; set; }

    public long ScheduleMetadataId { get; set; }

    public long PaperId { get; set; }

    public string Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public List<long> SchedulePaperSelectedFormsIds { get; set; }
}