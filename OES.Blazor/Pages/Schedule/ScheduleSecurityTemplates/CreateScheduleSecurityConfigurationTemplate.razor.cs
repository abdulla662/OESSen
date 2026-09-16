using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Schedule.ScheduleSecurityTemplates
{
    public partial class CreateScheduleSecurityConfigurationTemplate
    {
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazSecurityConfigurationsService BlazSecurityConfigurationService { get; set; }

        private AddOrUpdateSecurityConfigurationDto Model { get; set; } = new AddOrUpdateSecurityConfigurationDto();

        public async Task<bool> OnSubmitAsync()
        {
            if (ValidateForm())
            {
                await SaveTemplateAsync();
            }
            else
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
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

        private async Task SaveTemplateAsync()
        {
            var securityTemplate = MapToTemplate(Model);

            var response = await BlazSecurityConfigurationService.AddSecurityConfigurationsTemplateAsync(securityTemplate);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    Snackbar.Add(Resource.TemplateDataHasBeenCreatedSuccessfully, Severity.Success);
                    NavigationManager.NavigateTo("/ScheduleSecurityTemplates");
                    break;

                case HttpStatusCode.Conflict:
                    Snackbar.Add(response.Message, Severity.Warning);
                    break;

                default:
                    Snackbar.Add(Resource.FailedToCreateATemplatePleaseTryEnterAValidData, Severity.Error);
                    break;
            }
        }

        private static ScheduleSecurityConfigTempRequestDto MapToTemplate(AddOrUpdateSecurityConfigurationDto model)
        {
            return new ScheduleSecurityConfigTempRequestDto
            {
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
