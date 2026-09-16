using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AuditLogs;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.Dtos.AuditLogs;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.AuditLogs
{
    public class BlazAuditLogsService : IBlazAuditLogsService
    {
        private readonly IBlazGetCustomTableData<GetAuditLogsDto> _blazGetCustomTableData;
        private readonly IHttpClientHelper _httpClientHelper;

        public BlazAuditLogsService(
            IBlazGetCustomTableData<GetAuditLogsDto> blazGetCustomTableData,
            IHttpClientHelper httpClientHelper
        )
        {
            _blazGetCustomTableData = blazGetCustomTableData;
            _httpClientHelper = httpClientHelper;
        }

        public async Task<CustomTableData<GetAuditLogsDto>> GetAllAsync(PaginationSearchModel paginationSearch)
        {
            return await _blazGetCustomTableData.GetCustomTableData(paginationSearch, "api/AuditLogs/GetAll");
        }

        public async Task<bool> SaveBatchAsync(List<AuditLogsDto> logs)
        {
            var response = await _httpClientHelper.PostAsync(logs, "api/AuditLogs/SaveBatch");

            return response?.CustomCodeStatus == Helper.Enums.CustomCodeStatus.Success;
        }
    }
}
