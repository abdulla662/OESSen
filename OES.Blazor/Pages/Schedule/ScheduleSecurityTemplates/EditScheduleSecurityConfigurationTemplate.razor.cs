using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Schedule.ScheduleSecurityTemplates
{
    public partial class EditScheduleSecurityConfigurationTemplate
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IBlazSecurityConfigurationsService BlazSecurityConfigurationService { get; set; }

        private AddOrUpdateSecurityConfigurationDto Model { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            var id = await BlazSessionStorageService.GetValue<long>("PerformEditBtnClick");

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

        public async Task<bool> OnSubmitAsync()
        {
            if (ValidateForm())
            {
                await SaveEditedTemplateAsync();
            }
            else
            {
                Snackbar.Add(@Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
            }

            StateHasChanged();

            return false;
        }

        private bool ValidateForm()
        {
            var isNameValid = !string.IsNullOrWhiteSpace(Model.Name);

            var isSupportedVersionForSecuredBrowserValid = !string.IsNullOrWhiteSpace(Model.SupportedVersionForSecuredBrowser);

            var isEvidanceDurationPerSecondsValid = Model.EvidenceDurationPerSeconds >= 0;

            return isNameValid && isSupportedVersionForSecuredBrowserValid && isEvidanceDurationPerSecondsValid;
        }


        // Template Methods:

        private async Task SaveEditedTemplateAsync()
        {
            var securityTemplate = MapToTemplate(Model);

            var response = await BlazSecurityConfigurationService.EditSecurityConfigurationsTemplateAsync(securityTemplate);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    Snackbar.Add(Resource.TemplateEditedSuccessfully, Severity.Success);
                    NavigationManager.NavigateTo("/ScheduleSecurityTemplates");
                    break;

                case HttpStatusCode.Conflict:
                    Snackbar.Add(response.Message, Severity.Warning);
                    break;

                default:
                    Snackbar.Add(Resource.FailedToEditTemplate, Severity.Error);
                    break;
            }
        }

        private static ScheduleSecurityConfigTempRequestDto MapToTemplate(AddOrUpdateSecurityConfigurationDto model)
        {
            return new ScheduleSecurityConfigTempRequestDto
            {
                Id = model.Id,
                Name = model.Name,
                SecuredBrowser = model.SecuredBrowser,
                EnableLog = model.EnableLog,
                DisplayLog = model.DisplayLog,
                ScreenShots = model.ScreenShots,
                CamShot = model.CamShot,
                CandidateCameraAndCameraShots = model.CandidateCameraAndCameraShots,
                EvidenceDurationPerSeconds = model.EvidenceDurationPerSeconds,
                SuperVisorAndCandidateCameraShot = model.SuperVisorAndCandidateCameraShot,
                SuperVisorAndCandidateCombinedCameraShot = model.SuperVisorAndCandidateCombinedCameraShot,
                ScreenShotsOnSubmitOrSkip = model.ScreenShotsOnSubmitOrSkip,
                SystemWarningWhenNoFace = model.SystemWarningWhenNoFace,
                AudioRecording = model.AudioRecording,
                FocusOutWindowsMinimizedScreenShot = model.FocusOutWindowsMinimizedScreenShot,
                FocusOutWindowsMinimizedCameraShot = model.FocusOutWindowsMinimizedCameraShot,
                Proctoring = model.Proctoring,
                ProctoringWarning = model.ProctoringWarning,
                IsCandidateIdCardRequired = model.IsCandidateIdCardRequired,
                SupportedVersionForSecuredBrowser = model.SupportedVersionForSecuredBrowser,
                CloseApplicationPasswordRequired = model.CloseApplicationPasswordRequired,
                RestrictAccessRequire = model.RestrictAccessRequire,
                IsOnLineExam = model.IsOnLineExam,
                IsAutoSyncEnabled = model.IsAutoSyncEnabled,
                SyncScheduleTime = model.SyncScheduleTime
            };
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/ScheduleSecurityTemplates");
        }
    }
}
