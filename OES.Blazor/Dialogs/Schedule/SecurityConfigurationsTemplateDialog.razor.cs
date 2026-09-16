using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;

namespace OES.Blazor.Dialogs.Schedule;

public partial class SecurityConfigurationsTemplateDialog : ComponentBase
{
    [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
    [Inject] private IBlazSecurityConfigurationsService BlazSecurityConfigurationService { get; set; }

    private AddOrUpdateSecurityConfigurationDto Model { get; set; } = new AddOrUpdateSecurityConfigurationDto();

    protected override async Task OnInitializedAsync()
    {
        var id = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");

        var oldModel = await BlazSecurityConfigurationService.GetSecurityConfigurationsTemplateByIdAsync(id);

        var data = (ScheduleSecConfigResponseDto)oldModel.Data;

        FillSecurityConfiguration(data);
    }

    private void FillSecurityConfiguration(ScheduleSecConfigResponseDto dto)
    {
        Model.Id = dto.Id;
        Model.Name = dto.TemplateName;
        Model.SecuredBrowser = dto.SecuredBrowser;
        Model.EnableLog = dto.EnableLog;
        Model.DisplayLog = dto.DisplayLog;
        Model.AudioRecording = dto.AudioRecording;
        Model.CamShot = dto.CamShot;
        Model.CandidateCameraAndCameraShots = dto.CandidateCameraAndCameraShots;
        Model.CloseApplicationPasswordRequired = dto.CloseApplicationPasswordRequired;
        Model.EvidenceDurationPerSeconds = dto.EvidenceDurationPerSeconds;
        Model.FocusOutWindowsMinimizedCameraShot = dto.FocusOutWindowsMinimizedCameraShot;
        Model.FocusOutWindowsMinimizedScreenShot = dto.FocusOutWindowsMinimizedScreenShot;
        Model.IsCandidateIdCardRequired = dto.IsCandidateIdCardRequired;
        Model.IsOnLineExam = dto.IsOnLineExam;
        Model.IsAutoSyncEnabled = dto.IsAutoSyncEnabled;
        Model.SyncScheduleTime = dto.SyncScheduleTime;
        Model.Proctoring = dto.Proctoring;
        Model.ProctoringWarning = dto.ProctoringWarning;
        Model.RestrictAccessRequire = dto.RestrictAccessRequire;
        Model.ScreenShots = dto.ScreenShots;
        Model.ScreenShotsOnSubmitOrSkip = dto.ScreenShotsOnSubmitOrSkip;
        Model.SuperVisorAndCandidateCameraShot = dto.SuperVisorAndCandidateCameraShot;
        Model.SuperVisorAndCandidateCombinedCameraShot = dto.SuperVisorAndCandidateCombinedCameraShot;
        Model.SupportedVersionForSecuredBrowser = dto.SupportedVersionForSecuredBrowser;
        Model.SystemWarningWhenNoFace = dto.SystemWarningWhenNoFace;
    }
}