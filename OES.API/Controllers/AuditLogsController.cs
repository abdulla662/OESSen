using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.AuditLogs;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class AuditLogsController : OESBaseController
    {
        private readonly IAuditLogsService _auditLogsService;

        public AuditLogsController(IAuditLogsService auditLogsService)
        {
            _auditLogsService = auditLogsService;
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetAll")]
        public async Task<ApiResponse> GetAllAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _auditLogsService.GetAllAsync(paginationSearchModel);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("ExportExcel")]
        [DoNotEncrypt]
        public async Task<IActionResult> ExportExcelAsync(PaginationSearchModel paginationSearchModel)
        {
            var result = await _auditLogsService.ExportExcelAsync(paginationSearchModel);
            return File(result.Bytes, result.ContentType, result.FileName);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("SaveBatch")]
        public async Task<ApiResponse> SaveBatchAsync([FromBody] List<AuditLogsDto> dtos)
        {
            return await _auditLogsService.SaveBatchAsync(dtos);
        }
    }
}
