using SharedHelper.Enums;

namespace OES.Helper.Dtos.AppUserProfileDtos
{
    public class AppUserProfileRetrievalDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string EmailAddress { get; set; }
        public string CreationUser { get; set; }
        public string ModeficationUser { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? ModeficationDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool IsActive { get; set; }
        public string? OrganizationSignature { get; set; }
        public Guid ModuleUserID { get; set; }
        public long OrganizationId { get; set; }
        public Gender? Gender { get; set; }
    }
}
