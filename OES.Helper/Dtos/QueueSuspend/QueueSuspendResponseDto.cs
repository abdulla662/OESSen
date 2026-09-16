using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.QueueSuspend
{
    public class QueueSuspendResponseDto
    {
        [Display(Name = nameof(Resource.ServerName), ResourceType = typeof(Resource))]
        public string ServerName { get; set; }

        public bool IsPaperLocked { get; set; }

        [Display(Name = nameof(Resource.ServerIP), ResourceType = typeof(Resource))]
        public string ServerIp { get; set; }

        public long PaperId { get; set; }

        public bool IsSuspensionAccepted { get; set; }

        public string VenueCode { get; set; }

        public bool IsInitiallyActive { get; set; }

        [Display(Name = nameof(Resource.CurrentStatus), ResourceType = typeof(Resource))]
        public string CurrentStatus => IsSuspensionAccepted ? Resource.Suspended : (IsInitiallyActive ? Resource.Active : Resource.Synced);
    }
}
