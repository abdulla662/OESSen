using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.CandidatesResult;
using OES.Helper.Dtos.Results;
using SharedHelper.General;

namespace OES.Blazor.Pages.Results
{
    public partial class ResultsStatus
    {
        [Inject] IBlazCandidatesResultService BlazCandidatesResultService { get; set; }

        private DateRange _dateRange = new(DateTimeHelper.Now.Date, DateTimeHelper.Now.AddDays(5).Date);

        private bool _showTables = false;

        private List<ExamSeriesGroupDto> _examSeriesData = [];

        private List<VenueCodeGroupDto> _venueCodeData = [];

        private async Task ShowDetails()
        {
            if (_dateRange.Start is null || _dateRange.End is null)
                return;

            var response = await BlazCandidatesResultService
                .GetAttendanceReportsByDateRangeAsync(_dateRange.Start.Value, _dateRange.End.Value);

            var grouped = response.Data as AttendanceReportGroupedDto ?? new AttendanceReportGroupedDto();

            _examSeriesData = grouped.ExamSeriesData;
            _venueCodeData = grouped.VenueCodeData;
            _showTables = true;
        }
    }
}
