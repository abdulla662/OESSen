namespace OES.Core.Entities
{
    public class AppUserProfileSubject : BaseEntity<int>
    {
        public int SubjectId { get; set; }
        public Guid AppUserProfilesId { get; set; }
        public Subject Subjects { get; set; }
        public AppUserProfile AppUserProfiles { get; set; }
    }
}
