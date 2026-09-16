using OES.Helper.Dtos.AuditLogs;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IAuditLogsService
    {
        Task<ApiResponse> GetAllAsync(PaginationSearchModel paginationSearchModel);

        Task<ExcelFileResult> ExportExcelAsync(PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> SaveBatchAsync(List<AuditLogsDto> dtos);
    }
}
