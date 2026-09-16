namespace OES.Helper.Dtos.Schedule
{
    public class AutoSyncSettingsDto
    {
        public bool IsAutoSyncEnabled { get; set; }

        public TimeOnly? SyncScheduleTime { get; set; }
    }
}
