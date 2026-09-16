namespace OES.Helper.Dtos.Results
{
    public class AttendanceReportGroupedDto
    {
        public List<ExamSeriesGroupDto> ExamSeriesData { get; set; } = [];

        public List<VenueCodeGroupDto> VenueCodeData { get; set; } = [];
    }
}
