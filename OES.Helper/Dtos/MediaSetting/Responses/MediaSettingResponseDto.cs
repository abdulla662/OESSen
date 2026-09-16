using OES.Helper.Enums;

namespace OES.Helper.Dtos.MediaSetting.Responses
{
    public class MediaSettingResponseDto
    {
        public long Id { get; set; }
        public MediaCategory MediaCategory { get; set; }
        public string MediaCategoryName { get; set; }
        public int MaxSizeInKB { get; set; }
        public string CreationUser { get; set; }
        public long OrganizationId { get; set; }
        public bool CurrentlyUsed { get; set; }

        public MediaSettingResponseDto() { }

        public MediaSettingResponseDto(
            long id,
            MediaCategory mediaCategory,
            int maxSizeInKB,
            string creationUser = default,
            long organizationId = 0,
            bool currentlyUsed = false
        )
        {
            Id = id;
            MediaCategory = mediaCategory;
            MediaCategoryName = mediaCategory.ToString();
            MaxSizeInKB = maxSizeInKB;
            CreationUser = creationUser;
            OrganizationId = organizationId;
            CurrentlyUsed = currentlyUsed;
        }
    }
}