namespace OES.Helper.General
{
    public sealed record CandidateAnswersSyncSettings
    {
        public bool AutoSyncEnabled { get; init; }

        public string SyncScheduleTime { get; init; } = "06:30";
    }
}
