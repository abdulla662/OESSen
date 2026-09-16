using System.ComponentModel.DataAnnotations.Schema;
namespace OES.Core.Entities
{
    public class QuestionCategoryGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long QuestionCategoryId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(QuestionCategoryId))]
        public virtual QuestionCategory QuestionCategory { get; set; }
    }
}
