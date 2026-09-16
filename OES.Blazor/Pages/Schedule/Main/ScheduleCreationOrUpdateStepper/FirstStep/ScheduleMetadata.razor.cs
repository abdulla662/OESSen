using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.Schedule;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.FirstStep
{
    public partial class ScheduleMetadata : ComponentBase
    {
        [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; }
        [Inject] private IBlazQuestionLanguageService BlazLanguageService { get; set; }
        [Inject] private IBlazVenueService BlazVenueService { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private IBlazScheduleService BlazScheduleService { get; set; }
        [Inject] private IBlazGroupService BlazGroupService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private CRUD_Dto CrudDto { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }

        private ScheduleMetadataDto ScheduleModel { get; set; } = new();
        public GetScheduleMetadataResponseDto GetScheduleMetadataResponseDto { get; set; } = new();
        private bool IsSaveTemplateDisabled => !ValidateScheduleMetadataFormArguments();

        private List<GetOESGroupDto> SelectedGroups { get; set; } = [];
        private List<LanguageDto> Languages = [];
        private List<GetVenueResponseDto> Venues = [];
        private IEnumerable<LanguageDto> _selectedLanguages = [];
        private IEnumerable<GetVenueResponseDto> _selectedVenues = [];
        private List<GetSchedulePaperTimeConfiguration> _schedulePapers = [];
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private StepperOperationalMode _thisComponentCurrentOperationalMode = StepperOperationalMode.InsertionMode;
        private bool _isSubmitButtonHit = false;
        private bool _persistedVenuesCheckboxesDisabled = false;
        private bool _persistedLanguagesCheckboxesDisabled = false;


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            var languagesTask = BlazLanguageService.GetAllLanguagesAsync();

            var venuesTask = BlazVenueService.GetAllVenuesAsync();

            await Task.WhenAll(languagesTask, venuesTask);

            Languages = await languagesTask;

            Venues = await venuesTask;

            CrudDto.IsCreatePage = await BlazSessionStorageService.GetValue<bool>("IsCreatePage");

            var scheduleId = await BlazSessionStorageService.GetValue<long>("PerformEditBtnClick");

            if (!CrudDto.IsCreatePage && scheduleId > 0)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                await PrepareFirstStepForUpdateModeAsync(scheduleId);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && CultureInfo.CurrentUICulture.Name == LanguageCode.ARABIC_CODE)
            {
                await JS.InvokeVoidAsync("observeAmPm");
            }
        }

        private void OnScheduleNameChanged(string scheduleName)
        {
            ScheduleModel.Name = scheduleName;

            ScheduleModel.Code = scheduleName.Trim().Replace(" ", "-");

            StateHasChanged();
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnFirstStepScheduleMetadataSubmitAsync()
        {
            SetSubmitButtonState(true);

            if (ValidateScheduleMetadataFormArguments())
            {
                bool isValidDateTime = await ValidateStartAndEndDateTimesAsync();

                if (!isValidDateTime) return false;

                if (_thisComponentCurrentOperationalMode == StepperOperationalMode.InsertionMode)
                {
                    return await AddScheduleMetadataAsync();
                }
                else if (_thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode)
                {
                    return await UpdateScheduleMetadataAsync();
                }

                SetSubmitButtonState(false);
            }
            else
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
            }

            StateHasChanged();

            return false;
        }

        private async Task<bool> AddScheduleMetadataAsync()
        {
            var addScheduleMetadataRequestDto = new AddScheduleMetadataRequestDto
            {
                Name = ScheduleModel.Name,
                Code = ScheduleModel.Code,
                Description = ScheduleModel.Description,
                StartDate = ScheduleModel.StartDate.Value.Date.Add(ScheduleModel.StartTime.Value),
                EndDate = ScheduleModel.EndDate.Value.Date.Add(ScheduleModel.EndTime.Value),
                StartTime = (TimeSpan)ScheduleModel.StartTime,
                EndTime = (TimeSpan)ScheduleModel.EndTime,
                LanguageIds = _selectedLanguages?.Select(l => l.Id).ToList() ?? [],
                ExamVenueIds = _selectedVenues?.Select(l => l.Id).ToList() ?? [],
                ScheduleLocation = (ScheduleLocation)ScheduleModel.ScheduleLocation,
                OESGroupDtos = SelectedGroups?.ToList() ?? []
            };

            var response = await BlazScheduleService.AddScheduleMetadataAsync(addScheduleMetadataRequestDto);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                GetScheduleMetadataResponseDto = JsonSerializer.Deserialize<GetScheduleMetadataResponseDto>(response.Data.ToString(), _jsonSerializerOptions);

                ScheduleModel.Id = GetScheduleMetadataResponseDto.ScheduleMetadataId;

                Snackbar.Add(response.Message, Severity.Success);

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);

                return false;
            }
        }

        private async Task<bool> UpdateScheduleMetadataAsync()
        {
            var updateScheduleMetadataRequestDto = new UpdateScheduleMetadataRequestDto
            {
                Id = ScheduleModel.Id,
                Name = ScheduleModel.Name,
                Code = ScheduleModel.Code,
                Description = ScheduleModel.Description,
                StartDate = ScheduleModel.StartDate.Value.Date.Add(ScheduleModel.StartTime.Value),
                EndDate = ScheduleModel.EndDate.Value.Date.Add(ScheduleModel.EndTime.Value),
                StartTime = ScheduleModel.StartTime,
                EndTime = ScheduleModel.EndTime,
                LanguageIds = _selectedLanguages?.Select(l => l.Id).ToList() ?? [],
                ExamVenueIds = _selectedVenues?.Select(l => l.Id).ToList() ?? [],
                ScheduleLocation = (ScheduleLocation)ScheduleModel.ScheduleLocation,
                OESGroupDtos = [.. SelectedGroups]
            };

            var response = await BlazScheduleService.UpdateScheduleMetadataAsync(updateScheduleMetadataRequestDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                GetScheduleMetadataResponseDto = JsonSerializer.Deserialize<GetScheduleMetadataResponseDto>(response.Data.ToString(), _jsonSerializerOptions);

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

        private async Task ShowScheduleTemplateAsync()
        {
            var dialogOptions = new DialogOptions
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Medium,
            };

            var dialogParams = new DialogParameters();

            var dialog = await DialogService.ShowAsync<ScheduleTemplate>(string.Empty, dialogParams, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var scheduleTemplateId = (long)result.Data;

                await FillScheduleFromTemplateAsync(scheduleTemplateId);
            }
        }

        private async Task FillScheduleFromTemplateAsync(long scheduleTemplateId)
        {
            var response = await BlazScheduleService.GetScheduleTemplateByIdAsync(scheduleTemplateId);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var scheduleMetadataDto = (ScheduleMetadataRetrievalDto)response.Data;

                FillScheduleMetadata(scheduleMetadataDto);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void FillScheduleMetadata(ScheduleMetadataRetrievalDto scheduleMetadataDto)
        {
            ScheduleModel.Name = scheduleMetadataDto.Name;
            ScheduleModel.Code = scheduleMetadataDto.Code;
            ScheduleModel.Description = scheduleMetadataDto.Description;
            ScheduleModel.StartDate = scheduleMetadataDto.StartDate;
            ScheduleModel.StartTime = scheduleMetadataDto.StartTime;
            ScheduleModel.EndDate = scheduleMetadataDto.EndDate;
            ScheduleModel.EndTime = scheduleMetadataDto.EndTime;
            ScheduleModel.ScheduleLocation = scheduleMetadataDto.ScheduleLocation;
            _selectedLanguages = Languages.Where(l => scheduleMetadataDto.LanguageIds.Contains(l.Id)).ToList();
            _selectedVenues = Venues.Where(v => scheduleMetadataDto.ExamVenueIds.Contains(v.Id)).ToList();

            StateHasChanged();
        }

        private async Task OpenScheduleTemplateNameDialog()
        {
            var dialogOptions = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<ScheduleTemplateNameDialog>(Resource.SaveAsScheduleTemplate, dialogOptions);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string templateName)
            {
                await SaveScheduleTemplateAsync(templateName);
            }
        }

        private async Task SaveScheduleTemplateAsync(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                Snackbar.Add(Resource.TemplateNameIsRequired, Severity.Error);

                return;
            }

            var scheduleTemplateObject = new ScheduleCreationTemplateDto
            {
                TemplateName = templateName,
                Name = ScheduleModel.Name,
                Code = ScheduleModel.Code,
                Description = ScheduleModel.Description,
                StartDate = ScheduleModel.StartDate ?? DateTime.MinValue.Date,
                StartTime = ScheduleModel.StartTime ?? TimeSpan.Zero,
                EndDate = ScheduleModel.EndDate ?? DateTime.MinValue.Date,
                EndTime = ScheduleModel.EndTime ?? TimeSpan.Zero,
                ScheduleLocation = ScheduleModel.ScheduleLocation ?? ScheduleLocation.Central,
                LanguageIds = _selectedLanguages?.Select(l => l.Id).ToList() ?? [],
                ExamVenueIds = _selectedVenues?.Select(v => v.Id).ToList() ?? []
            };

            var response = await BlazScheduleService.AddScheduleTemplateAsync(scheduleTemplateObject);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            StateHasChanged();
        }


        // HELPER METHODS

        private async Task PrepareFirstStepForUpdateModeAsync(long scheduleId)
        {
            var response = await BlazScheduleService.GetScheduleByIdAsync(scheduleId);

            if (response != null)
            {
                ScheduleModel = new ScheduleMetadataDto
                {
                    Id = response.Id,
                    Name = response.Name,
                    Code = response.Code,
                    Description = response.Description,
                    StartDate = response.StartDate,
                    StartTime = response.StartTime,
                    EndDate = response.EndDate,
                    EndTime = response.EndTime,
                    ExamVenueIds = response.ExamVenueIds ?? [],
                    LanguageIds = response.LanguageIds ?? [],
                    ScheduleLocation = response.ScheduleLocation,
                };

                _selectedLanguages = [.. Languages.Where(result => ScheduleModel.LanguageIds.Contains(result.Id))];
                _selectedVenues = [.. Venues.Where(result => ScheduleModel.ExamVenueIds.Contains(result.Id))];
                SelectedGroups = response.OESGroupDtos?
                    .Where(g => !g.AutoCreatedForUser)
                    .ToList() ?? [];
            }
        }

        private bool ValidateScheduleMetadataFormArguments()
        {
            var isNameValid = !string.IsNullOrWhiteSpace(ScheduleModel.Name);
            var isCodeValid = !string.IsNullOrWhiteSpace(ScheduleModel.Code);
            var isDescriptionValid = !string.IsNullOrWhiteSpace(ScheduleModel.Description);
            var isLanguageValid = _selectedLanguages?.Any() == true;
            var isExamVenueValid = _selectedVenues?.Any() == true;
            var isStartDateValid = ScheduleModel.StartDate.HasValue;
            var isStartTimeValid = ScheduleModel.StartTime.HasValue;
            var isEndDateValid = ScheduleModel.EndDate.HasValue;
            var isEndTimeValid = ScheduleModel.EndTime.HasValue;
            var isSchedualLocationValid = ScheduleModel.ScheduleLocation != null && Enum.IsDefined(typeof(ScheduleLocation), ScheduleModel.ScheduleLocation);

            return isNameValid &&
                   isCodeValid &&
                   isDescriptionValid &&
                   isStartDateValid &&
                   isStartTimeValid &&
                   isEndDateValid &&
                   isEndTimeValid &&
                   isLanguageValid &&
                   isExamVenueValid &&
                   isSchedualLocationValid;
        }

        private async Task<bool> ValidateStartAndEndDateTimesAsync()
        {
            if (ScheduleModel.StartDate.HasValue &&
                ScheduleModel.StartTime.HasValue &&
                ScheduleModel.EndDate.HasValue &&
                ScheduleModel.EndTime.HasValue)
            {
                var scheduleStartDateTime = ScheduleModel.StartDate.Value.Date.Add(ScheduleModel.StartTime.Value);
                var scheduleEndDateTime = ScheduleModel.EndDate.Value.Date.Add(ScheduleModel.EndTime.Value);

                _schedulePapers = await BlazSchedulePaperService.GetSchedulePapersAsync(ScheduleModel.Id);

                if (_schedulePapers?.Count > 0)
                {
                    var outsidePapers = _schedulePapers.Where(p =>
                        p.StartDate.ToDateTime(p.StartTime) < scheduleStartDateTime ||
                        p.EndDate.ToDateTime(p.EndTime) > scheduleEndDateTime
                    )
                    .ToList();

                    if (outsidePapers?.Count > 0)
                    {
                        Snackbar.Add(Resource.CannotReduceSchedule, Severity.Error);
                        return false;
                    }
                }

                if (scheduleEndDateTime <= scheduleStartDateTime)
                {
                    Snackbar.Add(Resource.EndDateTimeMustBeAfterStartDateTime, Severity.Error);
                    return false;
                }

                return true;
            }

            return false;
        }

        private void SetSubmitButtonState(bool state)
        {
            _isSubmitButtonHit = state;

            StateHasChanged();
        }

        public void SetVenuesSelectionDisablingState()
        {
            _persistedVenuesCheckboxesDisabled = true;

            StateHasChanged();
        }

        public void SetLanguagesSelectionDisablingState()
        {
            _persistedLanguagesCheckboxesDisabled = true;

            StateHasChanged();
        }

        private async Task OpenGroupDialog()
        {
            if (_thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode)
            {
                var allowed = await AuthService.IsCurrentUserOwnerAsync(
                    ScheduleModel.Id,
                    BlazScheduleService.GetScheduleGroupsAsync,
                    dto => [dto.OwnerGroupId ?? Guid.Empty]
                );

                if (!allowed)
                {
                    Snackbar.Add(
                        string.Format(Resource.OnlyCreatorCanManageGroups, Resource.Schedule),
                        Severity.Error
                    );
                    return;
                }
            }

            var selected = await DialogInteractionService.OpenSelectionDialogAsync(
                dialogService: DialogService,
                title: Resource.SelectGroup,
                preSelectedItems: SelectedGroups.Where(g => !g.AutoCreatedForUser),
                endpointService: async (pagination) =>
                {
                    var allGroups = await BlazScheduleService.GetAllSchedulesCreatedByCurrentUser();

                    var filteredGroups = allGroups;

                    if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
                    {
                        filteredGroups = [.. allGroups.Where(g => g.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))];
                    }

                    return new CustomTableData<GetOESGroupDto>
                    {
                        Items = filteredGroups,
                        TotalItems = filteredGroups.Count
                    };
                },
                resourceType: ResourceType.Schedule,
                onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync)
            );

            SelectedGroups = selected
                ?.Where(g => !g.AutoCreatedForUser)
                .ToList() ?? [];

            StateHasChanged();
        }

        private async Task DeleteGroupAsync(Guid groupId)
        {
            var response = await BlazGroupService.DeleteGroupAsync(groupId);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
                Snackbar.Add(response.Message, Severity.Success);
            else
                Snackbar.Add(response.Message, Severity.Error);
        }
    }
}