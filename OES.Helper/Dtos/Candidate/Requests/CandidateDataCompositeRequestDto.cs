using Microsoft.AspNetCore.Components.Forms;

namespace OES.Helper.Dtos.Candidate.Requests
{
    public class CandidateDataCompositeRequestDto
    {
        public IBrowserFile File { get; set; }

        public string VenueCode { get; set; }

        public bool FromDumb { get; set; } = false;

        public long SchedulePaperId { get; set; }

        public List<string> ScheduleVenueCodes { get; set; } = [];
    }
}
