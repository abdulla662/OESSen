using OES.Helper.Dtos.Results;

namespace OES.Interface.Interfaces
{
    public interface IVerificationReportsService
    {
        Task<int> GetVenueCountAsync();

        IAsyncEnumerable<CentersAllocationVerificationResultDto> GetCentersAllocationVerificationReportStreamAsync(
            CentersAllocationVerificationRequest request,
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<VenueAttendanceVerificationResultDto> GetVenueAttendanceVerificationReportStreamAsync(
            VenueAttendanceVerificationRequest request,
            CancellationToken cancellationToken = default);
    }
}
