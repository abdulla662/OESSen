namespace OES.Helper.General
{
    public static class SignalRCommonConstant
    {
        // SYNC SPECIFIC CONSTANTS:

        public static string CommonScheduleGroup { get; } = nameof(CommonScheduleGroup);
        public static string UpdateJobStatus { get; } = nameof(UpdateJobStatus);
        public static string JoinScheduleGroup { get; } = nameof(JoinScheduleGroup);
        public static string RequestCurrentStatus { get; } = nameof(RequestCurrentStatus);
        public static string LeaveScheduleGroup { get; } = nameof(LeaveScheduleGroup);
    }
}