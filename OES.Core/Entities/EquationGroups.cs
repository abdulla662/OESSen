using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class EquationGroups : BaseEntity<long>
    {
        // Properties

        public Guid OESGroupId { get; set; }

        public long EquationId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(EquationId))]
        public virtual EquationTemplate Equation { get; set; }
    }
}
