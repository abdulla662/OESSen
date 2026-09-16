namespace OES.Helper.Dtos.QueueSuspend
{
    public class ManageFormSuspensionsDto
    {
        public long FormId { get; set; }

        public List<string> SuspendedVenueCodes { get; set; } = [];
    }
}
