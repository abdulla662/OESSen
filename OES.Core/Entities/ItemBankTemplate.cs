
namespace OES.Core.Entities
{
    public class ItemBankTemplate : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Data { get; set; }

        public bool IsFromItmBankAI { get; set; }
    }
}
