using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperSubject : BaseEntity<long>
    {
        public long PaperId { get; set; }

        public long SubjectId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual Subject Subject { get; set; }
    }
}
