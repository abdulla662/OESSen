namespace OES.Helper.Dtos.QueueSuspend
{
    public class ManagePaperSuspensionsDto
    {
        public long PaperId { get; set; }

        public List<string> SuspendedVenueCodes { get; set; } = [];
    }
}
