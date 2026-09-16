using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Schedule.Responses
{
    public class ScheduleSecConfigResponseDto
    {
        public long Id { get; set; }

        public string? TemplateName { get; set; } = string.Empty;

        public bool SecuredBrowser { get; set; }

        public bool EnableLog { get; set; }

        public bool DisplayLog { get; set; }

        public bool ScreenShots { get; set; }

        public bool CamShot { get; set; }

        public bool CandidateCameraAndCameraShots { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be at least 1 second")]
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

        public bool IsAutoSyncEnabled { get; set; }

        public TimeOnly? SyncScheduleTime { get; set; }
    }
}