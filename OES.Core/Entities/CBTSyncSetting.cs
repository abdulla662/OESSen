namespace OES.Core.Entities
{
    public class CBTSyncSetting : BaseEntity<long>
    {
        public bool CBTAutoSyncEnabled { get; set; }

        public string CBTSyncScheduleTime { get; set; }
    }
}
