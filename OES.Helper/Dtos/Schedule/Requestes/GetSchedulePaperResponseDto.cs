namespace OES.Helper.Dtos.Schedule.Requestes;

public class GetSchedulePaperResponseDto
{
    public long ScheduleId { get; set; }

    public long PaperId { get; set; }

    public string Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public List<long> SchedulePaperSelectedFormsIds { get; set; }

    public List<long> FormsWithNotSyncedCandidatesIds { get; set; } = [];
}