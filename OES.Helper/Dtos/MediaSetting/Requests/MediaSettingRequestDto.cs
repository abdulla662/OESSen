using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.MediaSetting.Requests
{
    public class MediaSettingRequestDto
    {
        [Required]
        public MediaCategory MediaCategory { get; set; }

        [Required]
        [Range(1, 30000, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.MediaMaxSizeValidation))]
        public int MaxSizeInKB { get; set; }
    }
}
