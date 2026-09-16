using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.CBTSyncSetting.Requests
{
    public class CBTSyncSettingRequestDto
    {
        public long Id { get; set; }

        public bool CBTAutoSyncEnabled { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.FieldRequired))]
        public string CBTSyncScheduleTime { get; set; }
    }
}
