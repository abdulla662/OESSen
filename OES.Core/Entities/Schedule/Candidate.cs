using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class Candidate : BaseEntity<long>
    {
        [Required] public string CandidateCode { get; set; }

        [Required] public string Name { get; set; }

        [Required] public string NationalId { get; set; }

        [Required] public string UserName { get; set; }

        [Required] public string Password { get; set; }

        public string Qualification { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Address { get; set; }

        [Required] public string Mobile { get; set; }

        [Required] public string Email { get; set; }

        public string PhotoURL { get; set; }

        public string SignatureURL { get; set; }

        [Required] public int Gender { get; set; }

        public string RegistrationCenterCode { get; set; }

        public DateTime? RegistrationDateTime { get; set; }

        public bool IsSynced { get; set; } = false;

        public bool HasDisability { get; set; }

        public long? DisabilityId { get; set; }


        // Navigational Properties

        public ICollection<SchedulePaperCandidate> SchedulePapers { get; set; } = [];

        public ICollection<CandidateOrganizationNodeLookupItem> CandidateOrganizationNodeLookupItems { get; set; } = [];

        [ForeignKey(nameof(DisabilityId))]
        public virtual Disability? Disability { get; set; }
    }
}
