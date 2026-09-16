using OES.Core.Entities.Schedule;

namespace OES.Core.Entities
{
    public class Disability : BaseEntity<long>
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public float ExtraTimePercentage { get; set; }


        // Navigational Properties

        public ICollection<Candidate> Candidates { get; set; } = [];
    }
}