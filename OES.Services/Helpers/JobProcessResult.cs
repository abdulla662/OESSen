using OES.Core.Entities;

namespace OES.Services.Helpers
{
    public class JobProcessResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public RealTimeSyncJob CreatedJob { get; set; }
    }
}
