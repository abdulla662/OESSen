using Microsoft.AspNetCore.Http;

namespace OES.Helper.Dtos.CandidateBatchImportHistory.Requests
{
    public class AddCandidateBatchImportHistoryRequestDto
    {
        public IFormFile File { get; set; }

        public long SchedulePaperId { get; set; }
    }
}
