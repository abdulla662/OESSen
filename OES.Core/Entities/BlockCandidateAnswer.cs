using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class BlockCandidateAnswer : BaseEntity<long>
    {
        public long CandidateId { get; set; }
        public string RegistrationNumber { get; set; }
        public long PaperId { get; set; }
        public long PaperFormId { get; set; }
        public long BlockId { get; set; }
        public long? CandidateExamTrialId { get; set; }
        public long ScheduleId { get; set; }
        public long SchedulePaperId { get; set; }
        public decimal TotalScore { get; set; }
        public int TotalQuestionsCount { get; set; }
        public int CorrectQuestionsCount { get; set; }
        public int IncorrectQuestionsCount { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool Synced { get; set; }
        public DateTime? SyncDate { get; set; }
        public string VenueCode { get; set; }
        public long VenueId { get; set; }

        // Navigational Properties
        //[ForeignKey(nameof(VenueId))]
        //public virtual Venue Venue { get; set; }
        [ForeignKey(nameof(BlockId))]
        public virtual Block Block { get; set; }
    }
}