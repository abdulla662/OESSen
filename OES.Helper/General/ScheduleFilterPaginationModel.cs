using OES.Helper.Enums;

namespace OES.Helper.General
{
    public class ScheduleFilterPaginationModel
    {
        public ScheduleLocation SelectedLocation { get; set; }

        public PublishingStatus SelectedStatus { get; set; }

        public SyncingStatus? SelectedSyncStatus { get; set; }
    }
}
