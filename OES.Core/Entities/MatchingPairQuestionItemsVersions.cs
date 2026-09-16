using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class MatchingPairQuestionItemsVersions : BaseEntity<long>
    {
        public long VersionNumber { get; set; }

        [Required]
        public string Body { get; set; }

        [Required]
        public int ColumnOrder { get; set; }

        public bool IsDataSource { get; set; }

        public long QuestionDetailsId { get; set; }
    }
}
