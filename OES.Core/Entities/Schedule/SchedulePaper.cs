using OES.Core.Entities.Paper;
using OES.Helper.Dtos.Paper.Responses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class SchedulePaper : BaseEntity<long>
    {
        public long ScheduleMetadataId { get; set; }

        public long PaperId { get; set; }

        public string Description { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(ScheduleMetadataId))]
        public ScheduleMetadata ScheduleMetadata { get; set; }

        [ForeignKey(nameof(PaperId))]
        public PaperMetadata PaperMetadata { get; set; }

        public SchedulePaperSettings PaperSettings { get; set; }

        public ICollection<SchedulePaperCandidate> Candidates { get; set; } = [];

        public ICollection<CandidateBatchImportHistory> CandidateBatchImportHistories { get; set; } = [];

        public ICollection<SchedulePaperForms> SchedulePaperForms { get; set; } = [];
    }
}
