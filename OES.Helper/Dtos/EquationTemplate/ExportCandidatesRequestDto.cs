
namespace OES.Helper.Dtos.EquationTemplate
{
    public class ExportCandidatesRequestDto
    {
        public long FormId { get; set; }
        public long VenueId { get; set; }
        public string? VenueCode { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? SentToCTR { get; set; }
        public List<string>? CandidateList { get; set; }
        public List<long> RegistrationIds { get; set; } = [];
    }
}