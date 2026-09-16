using OES.Helper.Dtos.OESUserGroups;
using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.Schedule.Responses
{
    public sealed record GetScheduleMetadataResponseDto
    {
        public long ScheduleMetadataId { get; private init; }

        public List<long> LanguagesIds { get; private init; } = [];

        public List<long> VenuesIds { get; private init; } = [];

        public DateOnly StartDate { get; private init; }

        public DateOnly EndDate { get; private init; }

        public TimeOnly StartTime { get; private init; }

        public TimeOnly EndTime { get; private init; }

        public List<GetOESGroupDto> OESGroupDtos { get; private init; } = [];

        public GetScheduleMetadataResponseDto() { }

        [JsonConstructor]
        public GetScheduleMetadataResponseDto(long scheduleMetadataId,
                                              List<long> languagesIds,
                                              List<long> venuesIds,
                                              DateOnly startDate,
                                              DateOnly endDate,
                                              TimeOnly startTime,
                                              TimeOnly endTime,
                                              List<GetOESGroupDto> oesGroupDtos)
        {
            ScheduleMetadataId = scheduleMetadataId;
            LanguagesIds = languagesIds;
            VenuesIds = venuesIds;
            StartDate = startDate;
            EndDate = endDate;
            StartTime = startTime;
            EndTime = endTime;
            OESGroupDtos = oesGroupDtos;
        }
    }
}
