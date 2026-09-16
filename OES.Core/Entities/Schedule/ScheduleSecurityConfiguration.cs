using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Schedule
{
    public class ScheduleSecurityConfiguration : BaseEntity<long>
    {
        public long ScheduleMetadataId { get; set; }

        public bool SecuredBrowser { get; set; }

        public bool EnableLog { get; set; }

        public bool DisplayLog { get; set; }

        public bool ScreenShots { get; set; }

        public bool CamShot { get; set; }

        public bool CandidateCameraAndCameraShots { get; set; }

        public long EvidenceDurationPerSeconds { get; set; }

        public bool SuperVisorAndCandidateCameraShot { get; set; }

        public bool SuperVisorAndCandidateCombinedCameraShot { get; set; }

        public bool ScreenShotsOnSubmitOrSkip { get; set; }

        public bool SystemWarningWhenNoFace { get; set; }

        public bool AudioRecording { get; set; }

        public bool FocusOutWindowsMinimizedScreenShot { get; set; }

        public bool FocusOutWindowsMinimizedCameraShot { get; set; }

        public bool Proctoring { get; set; }

        public bool ProctoringWarning { get; set; }

        public bool IsCandidateIdCardRequired { get; set; }

        public string SupportedVersionForSecuredBrowser { get; set; }

        public bool CloseApplicationPasswordRequired { get; set; }

        public bool RestrictAccessRequire { get; set; }

        public bool IsOnLineExam { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(ScheduleMetadataId))]
        public virtual ScheduleMetadata ScheduleMetadata { get; set; }
    }
}