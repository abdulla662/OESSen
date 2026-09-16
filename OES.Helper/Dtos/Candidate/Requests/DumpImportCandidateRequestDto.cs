using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Candidate.Requests
{
    public class DumpImportCandidateRequestDto : AddMultipleCandidateDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.VenueCodeIsRequired))]
        public string VenueCode { get; set; }
    }
}