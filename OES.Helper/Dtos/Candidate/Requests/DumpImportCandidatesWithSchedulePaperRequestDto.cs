using Microsoft.AspNetCore.Components.Forms;

namespace OES.Helper.Dtos.Candidate.Requests
{
    public class DumpImportCandidatesWithSchedulePaperRequestDto
    {
        public IBrowserFile File { get; set; } = default!;

        public long SchedulePaperId { get; set; }

        public List<string> ScheduleVenueCodes { get; set; } = [];
    }
}