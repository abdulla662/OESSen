using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class OESGroup : BaseEntity<Guid>
    {
        // Properties

        [MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        public bool IsTemplate { get; set; } = false;

        public bool IsPredefined { get; set; } = false;

        public bool AutoCreatedForUser { get; set; }


        // Navigational Properties

        public virtual ICollection<OESRole> Roles { get; set; } = [];

        public virtual ICollection<OESGroupResource> GroupResources { get; set; } = [];

        public virtual ICollection<ILOGroup> ILOGroups { get; set; } = [];

        public virtual ICollection<ItemBankGroups> ItemBankGroups { get; set; } = [];

        public virtual ICollection<AppUserProfileGroup> AppUserProfileGroups { get; set; } = [];

        public virtual ICollection<QuestionGroups> QuestionGroups { get; set; } = [];

        public virtual ICollection<PaperGroups> PaperGroups { get; set; } = [];

        public virtual ICollection<ScheduleGroups> ScheduleGroups { get; set; } = [];

        public virtual ICollection<BlockGroups> BlockGroups { get; set; } = [];

        public virtual ICollection<CandidateGroups> CandidateGroups { get; set; } = [];

        public virtual ICollection<VenueGroups> VenueGroups { get; set; } = [];

        public virtual ICollection<EquationGroups> EquationGroups { get; set; } = [];

        public virtual ICollection<TransitionProfileGroups> TransitionProfileGroups { get; set; } = [];

        public virtual ICollection<QuestionCategoryGroups> QuestionCategoryGroups { get; set; } = [];

        public virtual ICollection<DeltaTypeGroups> DeltaTypeGroups { get; set; } = [];

        public virtual ICollection<DifficultyProfileGroups> DifficultyProfileGroups { get; set; } = [];

        public virtual ICollection<DifficultyLevelGroups> DifficultyLevelGroups { get; set; } = [];

        public virtual ICollection<LanguageGroups> LanguageGroups { get; set; } = [];

        public virtual ICollection<SubjectGroups> SubjectGroups { get; set; } = [];

        public virtual ICollection<MediaSettingsGroups> MediaSettingsGroups { get; set; } = [];

        public virtual ICollection<QualityCheckCommitteeGroups> QualityCheckCommitteeGroups { get; set; } = [];
    }
}
