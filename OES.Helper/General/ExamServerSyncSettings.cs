namespace OES.Helper.General
{
    public sealed record ExamServerSyncSettings
    {
        public bool AutoSyncEnabled { get; init; }

        public string SyncScheduleTime { get; init; } = "03:00";

        public int MaxSchedulesPerRun { get; init; } = 3;
    }
}
