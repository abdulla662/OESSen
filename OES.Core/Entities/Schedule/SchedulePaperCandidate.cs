using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class SchedulePaperCandidate : BaseEntity<long>
    {
        public long CandidateId { get; set; }

        public long VenueId { get; set; }

        public long SchedulePaperId { get; set; }

        public long BatchId { get; set; }

        public long RegistrationNumber { get; set; }

        public long? PaperFormId { get; set; }

        public DateTime? CandidateExamDate { get; set; }

        public bool IsSynced { get; set; } = false;


        // Navigational Properties

        [ForeignKey(nameof(CandidateId))]
        public virtual Candidate Candidate { get; set; }

        [ForeignKey(nameof(VenueId))]
        public virtual Venue Venue { get; set; }

        [ForeignKey(nameof(SchedulePaperId))]
        public virtual SchedulePaper SchedulePaper { get; set; }

        [ForeignKey(nameof(BatchId))]
        public CandidateBatchImportHistory CandidateBatchImportHistory { get; set; }

        [ForeignKey(nameof(PaperFormId))]
        public PaperForm PaperForm { get; set; }

        public virtual ICollection<ScheduleGroups> ScheduleGroups { get; set; } = [];
    }
}
