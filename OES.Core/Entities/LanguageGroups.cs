using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class LanguageGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long LanguageId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }
    }
}
