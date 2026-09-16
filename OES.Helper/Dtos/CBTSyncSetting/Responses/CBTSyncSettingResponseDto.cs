namespace OES.Helper.Dtos.CBTSyncSetting.Responses
{
    public class CBTSyncSettingResponseDto
    {
        public long Id { get; set; }
        public bool CBTAutoSyncEnabled { get; set; }
        public string CBTSyncScheduleTime { get; set; }
        public string CBTSyncScheduleTimeFormatted => FormatToAmPm(CBTSyncScheduleTime);

        public CBTSyncSettingResponseDto() { }

        public CBTSyncSettingResponseDto(long id, bool cbtAutoSyncEnabled, string cbtSyncScheduleTime)
        {
            Id = id;
            CBTAutoSyncEnabled = cbtAutoSyncEnabled;
            CBTSyncScheduleTime = cbtSyncScheduleTime;
        }

        private static string FormatToAmPm(string time)
        {
            if (TimeSpan.TryParse(time, out var timeSpan))
                return DateTime.Today.Add(timeSpan).ToString("hh:mm tt");

            return time;
        }
    }
}
