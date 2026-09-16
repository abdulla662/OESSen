using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class OESResource : BaseEntity<long>
    {
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }


        // Navigational Properties

        public ICollection<OESGroupResource> GroupResources { get; set; } = [];
    }
}
