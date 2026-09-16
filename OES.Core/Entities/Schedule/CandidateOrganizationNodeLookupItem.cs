using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class CandidateOrganizationNodeLookupItem : BaseEntity<long>
    {
        public long CandidateId { get; set; }

        public long OrganizationNodeLookupItemId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(CandidateId))]
        public Candidate Candidate { get; set; }

        [ForeignKey(nameof(OrganizationNodeLookupItemId))]
        public OrganizationNodeLookupItem OrganizationNodeLookupItem { get; set; }
    }
}
