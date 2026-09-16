namespace OES.Core.Entities.CBTCandidates;

public class CBTCandidatesScyncJobs : BaseEntity<long>
{
    public string VenueCode { get; set; }
    public string CenterCode { get; set; }
    public DateTime SyncDate { get; set; }
    public DateTime JobStartTime { get; set; }
    public DateTime? JobEndTime { get; set; }
    public int TotalCandidates { get; set; }
    public string Request { get; set; }
    public bool IsSuccess { get; set; } = true;
    public int ErrorCode { get; set; } = 0;
    public string ErrorMessage { get; set; }
    public bool IsManualSync { get; set; } = false;
    public bool IsAutoSync { get; set; } = false;


    // Navigation property

    public virtual ICollection<CBTCandidatesRecievedData> ReceivedCandidates { get; set; } = [];
}