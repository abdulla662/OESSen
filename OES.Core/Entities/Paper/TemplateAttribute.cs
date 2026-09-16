using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class TemplateAttribute : BaseEntity<long>
    {
        public long TemplateTypeId { get; set; }

        public long AttributeId { get; set; }


        [ForeignKey(nameof(AttributeId))]
        public virtual Attribute Attribute { get; set; }

        [ForeignKey(nameof(TemplateTypeId))]
        public virtual TemplateType Template { get; set; }
    }
}
