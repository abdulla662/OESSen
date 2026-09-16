using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class PaperForm : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public long? PaperId { get; set; }

        public AvailabilityStatus FormStatus { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata Paper { get; set; }

        public virtual ICollection<Stage> Stages { get; set; } = [];

        public ICollection<StandardSection> Sections { get; set; } = [];

        public virtual ICollection<GeneratedFormQuestion> FormQuestions { get; set; } = [];
    }
}