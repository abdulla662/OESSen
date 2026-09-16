using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.Schedule.Requestes
{
    public class UpdateScheduleMetadataRequestDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public List<long> LanguageIds { get; set; } = [];

        public List<long> ExamVenueIds { get; set; } = [];

        public DateTime? StartDate { get; set; }

        public TimeSpan? StartTime { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? EndTime { get; set; }

        public ScheduleLocation ScheduleLocation { get; set; }

        public List<GetOESGroupDto> OESGroupDtos { get; set; } = [];

        public DateTime? FullStartDateTime
        {
            get => StartDate.HasValue && StartTime.HasValue ? StartDate.Value.Date + StartTime.Value : null;
            set
            {
                if (value.HasValue)
                {
                    StartDate = value.Value.Date;
                    StartTime = value.Value.TimeOfDay;
                }
            }
        }

        public DateTime? FullEndDateTime
        {
            get => EndDate.HasValue && EndTime.HasValue ? EndDate.Value.Date + EndTime.Value : null;
            set
            {
                if (value.HasValue)
                {
                    EndDate = value.Value.Date;
                    EndTime = value.Value.TimeOfDay;
                }
            }
        }
    }
}