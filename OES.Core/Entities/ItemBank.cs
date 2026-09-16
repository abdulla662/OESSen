using OES.Core.Entities.Paper;
using OES.Core.Entities.QuestionQualityCheck;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class ItemBank : BaseEntity<long>
    {
        public string Name { get; set; }

        public string Code { get; set; }

        public long? ParentId { get; set; } = null!;

        [ForeignKey(nameof(ParentId))]
        public virtual ItemBank ParentItemBank { get; set; }

        public string ItemBankSignature { get; set; }

        public float Hours { get; set; }

        public string Description { get; set; }

        public bool Unscored { get; set; }

        public long? LevelId { get; set; }

        [ForeignKey(nameof(LevelId))]
        public ItemBankLevel Levels { get; set; }


        // Navigational Properties

        public virtual ICollection<ItemBank> Childreen { get; set; } = [];

        public virtual ICollection<QuestionMetadata> QuestionMetadata { get; set; } = [];

        public virtual ICollection<ItemBankGroups> ItemBankGroups { get; set; } = [];

        public virtual ICollection<PaperItemBankPoint> ItemBankPoints { get; set; }

        public virtual ICollection<QualityCheckCommitteeItemBank> QualityCheckCommitteeItemBanks { get; set; } = [];
    }
}
