namespace OES.Core.Entities
{
    public class EquationTemplate : BaseEntity<long>
    {
        public string Name { get; set; }

        public string TotalEquation { get; set; }


        // Navigation property

        public ICollection<EquationCategory> EquationCategories { get; set; }

        public ICollection<PaperItemBankEquation> PaperItemBankEquation { get; set; }

        public virtual ICollection<EquationGroups> EquationGroups { get; set; } = [];
    }
}
