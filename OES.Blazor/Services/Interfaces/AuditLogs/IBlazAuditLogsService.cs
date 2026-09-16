using OES.Helper.Dtos.AuditLogs;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.AuditLogs
{
    public interface IBlazAuditLogsService
    {
        Task<CustomTableData<GetAuditLogsDto>> GetAllAsync(PaginationSearchModel paginationSearch);

        Task<bool> SaveBatchAsync(List<AuditLogsDto> logs);
    }
}
