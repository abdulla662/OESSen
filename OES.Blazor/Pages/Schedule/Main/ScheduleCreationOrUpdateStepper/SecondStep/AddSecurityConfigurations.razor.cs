using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Dialogs.Schedule;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService;
using OES.Helper.Dtos.Schedule;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.SecondStep
{
    public partial class AddSecurityConfigurations : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private IBlazSecurityConfigurationsService BlazSecurityConfigurationService { get; set; }

        [Inject] private IDialogService DialogService { get; set; } = default!;

        [Inject] private ISyncToExamServer SyncToExamServer { get; set; } = default!;

        [Parameter] public long ScheduleId { get; set; }

        public string TemplateName { get; set; }

        public ScheduleSecurityConfigTempRequestDto ScheduleTemplate { get; set; } = new();

        private AddOrUpdateSecurityConfigurationDto Model { get; set; } = new AddOrUpdateSecurityConfigurationDto();

        private bool IsSaveTemplateDisabled =>
            Model.EvidenceDurationPerSeconds < 1 ||
            string.IsNullOrWhiteSpace(Model.SupportedVersionForSecuredBrowser);

        private bool _isSubmitButtonHit;

        private StepperOperationalMode _thisComponentCurrentOperationalMode = StepperOperationalMode.InsertionMode;

        private TimeSpan? _syncScheduleTimeSpan;


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            var response = await BlazSecurityConfigurationService.GetSecurityConfigurationsByScheduleIdAsync(ScheduleId);

            if (response != null)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                PrepareSecondStepModelForUpdate(response);
            }

            Model.ScheduleId = ScheduleId;

            await LoadAutoSyncStatusAsync();
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnSecondStepSecurityConfigurationsSubmitAsync()
        {
            SetSubmitButtonState(true);

            if (ValidateSecurityConfigurationsFormArguments())
            {
                Model.SyncScheduleTime = _syncScheduleTimeSpan.HasValue ? TimeOnly.FromTimeSpan(_syncScheduleTimeSpan.Value) : null;

                if (_thisComponentCurrentOperationalMode == StepperOperationalMode.InsertionMode)
                {
                    return await AddSecurityConfigurationsAsync(Model);
                }
                else if (_thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode)
                {
                    return await UpdateSecurityConfigurationsAsync(Model);
                }

                SetSubmitButtonState(false);
            }

            StateHasChanged();

            return false;
        }

        private async Task<bool> UpdateSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto addSecurityConfigurationDto)
        {
            var response = await BlazSecurityConfigurationService.UpdateSecurityConfigurationsAsync(addSecurityConfigurationDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
                return false;
            }
        }

        private async Task<bool> AddSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto updateSecurityConfigurationDto)
        {
            var response = await BlazSecurityConfigurationService.AddSecurityConfigurationsAsync(updateSecurityConfigurationDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                Snackbar.Add(response.Message, Severity.Success);
                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
                return false;
            }
        }


        // TEMPLATE METHODS:

        private async Task OpenTemplateNameDialog()
        {
            var parameters = new DialogParameters();

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.TemplateName, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string templateName)
            {
                TemplateName = templateName;

                await SaveTemplateAsync();
            }
        }

        private async Task SaveTemplateAsync()
        {
            ScheduleTemplate = MapToTemplate(Model, TemplateName);

            var response = await BlazSecurityConfigurationService.AddSecurityConfigurationsTemplateAsync(ScheduleTemplate);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    Snackbar.Add(Resource.TemplateDataHasBeenCreatedSuccessfully, Severity.Success);
                    break;

                case HttpStatusCode.Conflict:
                    Snackbar.Add(response.Message, Severity.Warning);
                    break;

                default:
                    Snackbar.Add(Resource.FailedToCreateATemplatePleaseTryEnterAValidData, Severity.Error);
                    break;
            }

            StateHasChanged();
        }

        private async Task ShowTemplatesAsync()
        {
            var dialogOptions = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
            };

            var dialogParams = new DialogParameters();

            var dialog = await DialogService.ShowAsync<SecurityConfigurationsTemplatesDialog>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var securityTemplateId = (long)result.Data;

                await FillSecondStepModelFromTemplateAysnc(securityTemplateId);
            }
        }


        // HELPER METHODS:

        private async Task LoadAutoSyncStatusAsync()
        {
            var response = await SyncToExamServer.GetAutoSyncStatusAsync(ScheduleId);

            if (response.StatusCode == HttpStatusCode.OK && response.Data != null)
            {
                var settings = (AutoSyncSettingsDto)response.Data;

                if (settings != null)
                {
                    Model.IsAutoSyncEnabled = settings.IsAutoSyncEnabled;
                    Model.SyncScheduleTime = settings.SyncScheduleTime;
                    _syncScheduleTimeSpan = settings.SyncScheduleTime.HasValue ? settings.SyncScheduleTime.Value.ToTimeSpan() : new TimeSpan(3, 0, 0);
                }
            }
        }

        private void PrepareSecondStepModelForUpdate(ExamSecurityConfigurationDto response)
        {
            Model = new AddOrUpdateSecurityConfigurationDto
            {
                Id = response.Id,
                ScheduleId = response.ScheduleMetadataId,
                SecuredBrowser = response.SecuredBrowser,
                EnableLog = response.EnableLog,
                DisplayLog = response.DisplayLog,
                AudioRecording = response.AudioRecording,
                CamShot = response.CamShot,
                CandidateCameraAndCameraShots = response.CandidateCameraAndCameraShots,
                CloseApplicationPasswordRequired = response.CloseApplicationPasswordRequired,
                EvidenceDurationPerSeconds = response.EvidenceDurationPerSeconds,
                FocusOutWindowsMinimizedCameraShot = response.FocusOutWindowsMinimizedCameraShot,
                FocusOutWindowsMinimizedScreenShot = response.FocusOutWindowsMinimizedScreenShot,
                IsCandidateIdCardRequired = response.IsCandidateIdCardRequired,
                IsOnLineExam = response.IsOnLineExam,
                Proctoring = response.Proctoring,
                ProctoringWarning = response.ProctoringWarning,
                RestrictAccessRequire = response.RestrictAccessRequire,
                ScreenShots = response.ScreenShots,
                ScreenShotsOnSubmitOrSkip = response.ScreenShotsOnSubmitOrSkip,
                SuperVisorAndCandidateCameraShot = response.SuperVisorAndCandidateCameraShot,
                SuperVisorAndCandidateCombinedCameraShot = response.SuperVisorAndCandidateCombinedCameraShot,
                SupportedVersionForSecuredBrowser = response.SupportedVersionForSecuredBrowser,
                SystemWarningWhenNoFace = response.SystemWarningWhenNoFace
            };
        }

        private bool ValidateSecurityConfigurationsFormArguments()
        {
            var isEvidenceDurationPerSecondsValid = Model.EvidenceDurationPerSeconds > 0;

            if (!isEvidenceDurationPerSecondsValid)
            {
                Snackbar.Add(Resource.EvidenceDurationMustBeGreaterThanZero, Severity.Error);
                return false;
            }

            var isSupportedVersionForSecuredBrowserValid = !string.IsNullOrWhiteSpace(Model.SupportedVersionForSecuredBrowser);

            if (!isSupportedVersionForSecuredBrowserValid)
            {
                Snackbar.Add(Resource.SupportedBrowserVersionIsRequired, Severity.Error);
                return false;
            }

            return true;
        }

        private async Task FillSecondStepModelFromTemplateAysnc(long templateId)
        {
            var response = await BlazSecurityConfigurationService.GetSecurityConfigurationsTemplateByIdAsync(templateId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var securityConfigurationsDto = (ScheduleSecConfigResponseDto)response.Data;

                Model.SecuredBrowser = securityConfigurationsDto.SecuredBrowser;
                Model.EnableLog = securityConfigurationsDto.EnableLog;
                Model.DisplayLog = securityConfigurationsDto.DisplayLog;
                Model.AudioRecording = securityConfigurationsDto.AudioRecording;
                Model.CamShot = securityConfigurationsDto.CamShot;
                Model.CandidateCameraAndCameraShots = securityConfigurationsDto.CandidateCameraAndCameraShots;
                Model.CloseApplicationPasswordRequired = securityConfigurationsDto.CloseApplicationPasswordRequired;
                Model.EvidenceDurationPerSeconds = securityConfigurationsDto.EvidenceDurationPerSeconds;
                Model.FocusOutWindowsMinimizedCameraShot = securityConfigurationsDto.FocusOutWindowsMinimizedCameraShot;
                Model.FocusOutWindowsMinimizedScreenShot = securityConfigurationsDto.FocusOutWindowsMinimizedScreenShot;
                Model.IsCandidateIdCardRequired = securityConfigurationsDto.IsCandidateIdCardRequired;
                Model.IsOnLineExam = securityConfigurationsDto.IsOnLineExam;
                Model.Proctoring = securityConfigurationsDto.Proctoring;
                Model.ProctoringWarning = securityConfigurationsDto.ProctoringWarning;
                Model.RestrictAccessRequire = securityConfigurationsDto.RestrictAccessRequire;
                Model.ScreenShots = securityConfigurationsDto.ScreenShots;
                Model.ScreenShotsOnSubmitOrSkip = securityConfigurationsDto.ScreenShotsOnSubmitOrSkip;
                Model.SuperVisorAndCandidateCameraShot = securityConfigurationsDto.SuperVisorAndCandidateCameraShot;
                Model.SuperVisorAndCandidateCombinedCameraShot = securityConfigurationsDto.SuperVisorAndCandidateCombinedCameraShot;
                Model.SupportedVersionForSecuredBrowser = securityConfigurationsDto.SupportedVersionForSecuredBrowser;
                Model.SystemWarningWhenNoFace = securityConfigurationsDto.SystemWarningWhenNoFace;
                Model.IsAutoSyncEnabled = securityConfigurationsDto.IsAutoSyncEnabled;
                Model.SyncScheduleTime = securityConfigurationsDto.SyncScheduleTime;
                _syncScheduleTimeSpan = Model.SyncScheduleTime.HasValue ? Model.SyncScheduleTime.Value.ToTimeSpan() : new TimeSpan(3, 0, 0);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private ScheduleSecurityConfigTempRequestDto MapToTemplate(AddOrUpdateSecurityConfigurationDto model, string name)
        {
            return new ScheduleSecurityConfigTempRequestDto
            {
                Name = name,
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
                SyncScheduleTime = _syncScheduleTimeSpan.HasValue ? TimeOnly.FromTimeSpan(_syncScheduleTimeSpan.Value) : null
            };
        }

        private void SetSubmitButtonState(bool state)
        {
            _isSubmitButtonHit = state;

            StateHasChanged();
        }
    }
}