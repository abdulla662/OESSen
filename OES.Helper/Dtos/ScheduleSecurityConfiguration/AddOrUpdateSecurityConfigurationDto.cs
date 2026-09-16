using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.ScheduleSecurityConfiguration
{
    public class AddOrUpdateSecurityConfigurationDto
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Template name is required")]
        public string Name { get; set; } = "Schedule Security Template 1";

        public long ScheduleId { get; set; }

        public bool SecuredBrowser { get; set; }

        public bool EnableLog { get; set; }

        public bool DisplayLog { get; set; }

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 second")]
        public long EvidenceDurationPerSeconds { get; set; } = 10;

        [Required(ErrorMessage = "Supported browser version is required")]
        public string SupportedVersionForSecuredBrowser { get; set; } = nameof(SupportedBrowserVersion.V1);

        public bool ScreenShots { get; set; }

        public bool CamShot { get; set; }

        public bool CandidateCameraAndCameraShots { get; set; }

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

        public bool CloseApplicationPasswordRequired { get; set; }

        public bool RestrictAccessRequire { get; set; }

        public bool IsOnLineExam { get; set; }

        public bool IsAutoSyncEnabled { get; set; }

        public TimeOnly? SyncScheduleTime { get; set; }
    }
}