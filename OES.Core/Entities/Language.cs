using OES.Core.Entities.Paper;
using OES.Core.Entities.Schedule;
using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class Language : BaseEntity<long>
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(5), AllowedValues("RTL", "LTR")]
        public string LanguageDirection { get; set; }


        // Navigational Properties

        public ICollection<ScheduleLanguage> Schedules { get; set; } = [];

        public virtual ICollection<Block> Blocks { get; set; } = [];
    }
}
