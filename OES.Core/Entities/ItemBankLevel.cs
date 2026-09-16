namespace OES.Core.Entities
{
    public class ItemBankLevel : BaseEntity<long>
    {
        public string Name { get; set; }

        public virtual ICollection<ItemBank> ItemBanks { get; set; } = new List<ItemBank>();
    }
}
