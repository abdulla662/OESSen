using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class Template : BaseEntity<long>
    {
        public long TemplateTypeId { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(TemplateTypeId))]
        public virtual TemplateType TemplateType { get; set; }

        public virtual ICollection<AdaptiveSection> AdaptiveSections { get; set; } = [];
    }
}
