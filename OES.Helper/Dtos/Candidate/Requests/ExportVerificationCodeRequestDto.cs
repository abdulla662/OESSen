using OES.Helper.Enums;

namespace OES.Helper.Dtos.Candidate.Requests
{
    public class ExportVerificationCodeRequestDto
    {
        public List<long>? VenueIds { get; set; }

        public ExamDateFilter ExamDate { get; set; }
    }
}
