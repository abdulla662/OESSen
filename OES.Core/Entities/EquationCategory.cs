using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class EquationCategory : BaseEntity<long>
    {
        public long EquationTemplateId { get; set; }

        public string CategoryName { get; set; }

        public string Equation { get; set; }

        public bool ShowInResults { get; set; } = true;


        // Navigation property

        [ForeignKey(nameof(EquationTemplateId))]
        public EquationTemplate EquationTemplate { get; set; }
    }
}
