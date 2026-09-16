namespace OES.Core.Entities.Paper
{
    public class TemplateType : BaseEntity<long>
    {
        public string Type { get; set; }

        public virtual ICollection<TemplateAttribute> TemplateAttributes { get; set; }

        public virtual ICollection<Template> Templates { get; set; }
    }
}
