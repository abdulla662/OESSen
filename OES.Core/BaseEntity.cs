using System.ComponentModel.DataAnnotations;


namespace OES.Core
{
    public class BaseEntity<T> : IBaseEntity<T> where T : struct
    {
        [Key]
        public T Id { get; set; }

        [Required]
        public string CreationUser { get; set; }

        public string ModeficationUser { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        public DateTime? ModeficationDate { get; set; } = null!;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedDate { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public string? OrganizationSignature { get; set; } = null;

        public long OrganizationId { get; set; }
    }
}
