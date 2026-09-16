using OES.Core.Entities.Schedule;

using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class CandidateGroups : BaseEntity<long>
    {
        // Properties

        public Guid OESGroupId { get; set; }

        public long CandidateId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(CandidateId))]
        public virtual Candidate Candidate { get; set; }
    }
}
