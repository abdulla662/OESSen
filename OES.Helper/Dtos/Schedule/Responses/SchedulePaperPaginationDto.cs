using OES.Helper.ResourceFiles;

namespace OES.Helper.Dtos.Schedule.Responses
{
    public class SchedulePaperPaginationDto
    {
        public long Id { get; set; }

        public long PaperId { get; set; }

        public string PaperName { get; set; }

        public string PaperCode { get; set; }

        public string SessionDescription { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public bool PaperSettingsConfigured { get; set; }

        public string PaperSettingsConfiguredDescription => PaperSettingsConfigured ? Resource.Configured : Resource.NotYet;

        public long CandidatesCount { get; set; }
    }
}