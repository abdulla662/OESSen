using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class CandidateBatchImportHistory : BaseEntity<long>
    {
        public string Name { get; set; }

        public Guid FileId { get; set; }

        public long SchedulePaperId { get; set; }

        public bool IsReversed { get; set; }

        public DataSource DataSource { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(SchedulePaperId))]
        public SchedulePaper SchedulePaper { get; set; }

        public ICollection<SchedulePaperCandidate> Candidates { get; set; } = [];
    }
}
