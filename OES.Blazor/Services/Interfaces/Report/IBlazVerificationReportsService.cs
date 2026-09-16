using OES.Helper.Dtos.Results;

namespace OES.Blazor.Services.Interfaces.Report
{
    public interface IBlazVerificationReportsService
    {
        Task<int> GetVenueCountAsync(CancellationToken cancellationToken = default);

        IAsyncEnumerable<CentersAllocationVerificationResultDto> GetCentersAllocationVerificationReportStreamAsync(
            CentersAllocationVerificationRequest request,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<VenueAttendanceVerificationResultDto> GetVenueAttendanceVerificationReportStreamAsync(
            VenueAttendanceVerificationRequest request,
            CancellationToken cancellationToken = default);
    }
}
