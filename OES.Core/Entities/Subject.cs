using OES.Core.Entities.Paper;
using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class Subject : BaseEntity<long>
    {
        [Required]
        public string Name { get; set; }


        // Navigational Properties

        public virtual ICollection<PaperSubject> Papers { get; set; }
    }
}
