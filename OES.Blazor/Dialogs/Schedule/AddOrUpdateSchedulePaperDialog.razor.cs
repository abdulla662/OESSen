using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Pages.Schedule.Main.ScheduleCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Globalization;
using System.Net;

namespace OES.Blazor.Dialogs.Schedule;

public partial class AddOrUpdateSchedulePaperDialog : ComponentBase
{
    [Inject] private IBlazPaperService BlazPaperService { get; set; }

    [Inject] private IBlazFormService BlazFormService { get; set; }

    [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; }

    [Inject] private ISnackbar Snackbar { get; set; }

    [Inject] private IJSRuntime JS { get; set; }

    [CascadingParameter] public MudDialogInstance MudDialog { get; set; }

    [Parameter] public long SchedulePaperId { get; set; } = 0;

    [Parameter] public ScheduleMetadataResultedParamsDto ScheduleMetadataResultedParamsDto { get; set; }

    [Parameter] public List<SchedulePaperPaginationDto> CurrentSchedulePapersList { get; set; } = [];


    private AddOrUpdateSchedulePaperRequestDto Model { get; set; } = new();


    private List<UserPaperForSchedule> _availablePapers = [];

    private List<GetFormDto> _forms = [];

    private List<GetFormDto> _selectedForms = [];

    private List<long> _formsWithNotSyncedCandidatesIds = [];

    private UserPaperForSchedule _selectedPaper;

    private DateTime? _selectedStartDate, _selectedEndDate;

    private TimeSpan? _selectedStartTime, _selectedEndTime;

    private bool _isSubmitButtonHit;


    // DATA PROCESSING

    protected override async Task OnInitializedAsync()
    {
        _availablePapers = await BlazPaperService.GetAllUserPapersAsync(ScheduleMetadataResultedParamsDto.ScheduleMetadataId);

        if (SchedulePaperId > 0)
        {
            var schedulePaperDto = await BlazSchedulePaperService.GetSchedulePaperByIdAsync(SchedulePaperId);

            if (schedulePaperDto != null)
            {
                _selectedPaper = _availablePapers.Find(p => p.Id == schedulePaperDto.PaperId);
                Model.Description = schedulePaperDto.Description;
                _selectedStartDate = schedulePaperDto.StartDate.ToDateTime(TimeOnly.MinValue);
                _selectedEndDate = schedulePaperDto.EndDate.ToDateTime(TimeOnly.MinValue);
                _selectedStartTime = schedulePaperDto.StartTime.ToTimeSpan();
                _selectedEndTime = schedulePaperDto.EndTime.ToTimeSpan();
                _formsWithNotSyncedCandidatesIds = schedulePaperDto.FormsWithNotSyncedCandidatesIds ?? [];
            }

            if (_selectedPaper != null)
            {
                _forms = await BlazFormService.GetAllFormsByPaperIdAsync(_selectedPaper.Id);

                _selectedForms = [.. _forms.Where(f => schedulePaperDto.SchedulePaperSelectedFormsIds.Contains(f.Id))];
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && CultureInfo.CurrentUICulture.Name == LanguageCode.ARABIC_CODE)
        {
            await JS.InvokeVoidAsync("observeAmPm");
        }
    }

    private void OnFormsSelectionChanged(IEnumerable<GetFormDto> selectedForms)
    {
        var selectedList = selectedForms.ToList();

        // Ensure forms, with not-synced candidates, cannot be deselected
        foreach (var form in _forms)
        {
            if (_formsWithNotSyncedCandidatesIds.Contains(form.Id) && !selectedList.Any(f => f.Id == form.Id))
            {
                selectedList.Add(form);
            }
        }

        _selectedForms = selectedList;

        StateHasChanged();
    }

    private async Task OnPaperSelected(UserPaperForSchedule userPaperForSchedule)
    {
        _selectedPaper = userPaperForSchedule;
        _selectedForms = [];
        _forms = _selectedPaper is not null
            ? await BlazFormService.GetAllFormsByPaperIdAsync(_selectedPaper.Id)
            : [];

        StateHasChanged();
    }

    private static async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
    {
        await Task.Delay(250);

        if (string.IsNullOrEmpty(value))
            return list;

        return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task<IEnumerable<UserPaperForSchedule>> SearchPapersAsync(string value, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(value))
            return _availablePapers;

        return await FilterListAsync(_availablePapers, p => p.Name, value);
    }


    // FORM SUBMIT METHODS

    private async Task OnSubmitButtonHitAsync()
    {
        if (SchedulePaperId > 0)
        {
            await UpdateSchedulePaperAsync();
        }
        else
        {
            await AddSchedulePaperAsync();
        }
    }

    private async Task AddSchedulePaperAsync()
    {
        _isSubmitButtonHit = true;

        if (ValidateSchedulePaperFormArguments())
        {
            if (!ValidateStartAndEndDateTimes()) return;

            if (!ValidateSamePaperDateTimeIntersections()) return;

            Model.ScheduleMetadataId = ScheduleMetadataResultedParamsDto.ScheduleMetadataId;
            Model.PaperId = _selectedPaper.Id;
            Model.StartDate = _selectedStartDate.HasValue ? DateOnly.FromDateTime(_selectedStartDate.Value) : new();
            Model.StartTime = _selectedStartTime.HasValue ? TimeOnly.FromTimeSpan(_selectedStartTime.Value) : new();
            Model.EndDate = _selectedEndDate.HasValue ? DateOnly.FromDateTime(_selectedEndDate.Value) : new();
            Model.EndTime = _selectedEndTime.HasValue ? TimeOnly.FromTimeSpan(_selectedEndTime.Value) : new();
            Model.SchedulePaperSelectedFormsIds = _selectedForms.ConvertAll(x => x.Id);

            var response = await BlazSchedulePaperService.AddSchedulePaperAsync(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _isSubmitButtonHit = false;

                Snackbar.Add(response.Message, Severity.Success);

                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }
        else
        {
            Snackbar.Add(Resource.PleaseFillRequiredFieldsCorrectly, Severity.Error);
        }

        StateHasChanged();
    }

    private async Task UpdateSchedulePaperAsync()
    {
        _isSubmitButtonHit = true;

        if (ValidateSchedulePaperFormArguments())
        {
            if (!ValidateStartAndEndDateTimes()) return;

            if (!ValidateSamePaperDateTimeIntersections()) return;

            Model.ScheduleMetadataId = ScheduleMetadataResultedParamsDto.ScheduleMetadataId;
            Model.SchedulePaperId = SchedulePaperId;
            Model.PaperId = _selectedPaper.Id;
            Model.StartDate = _selectedStartDate.HasValue ? DateOnly.FromDateTime(_selectedStartDate.Value) : new();
            Model.StartTime = _selectedStartTime.HasValue ? TimeOnly.FromTimeSpan(_selectedStartTime.Value) : new();
            Model.EndDate = _selectedEndDate.HasValue ? DateOnly.FromDateTime(_selectedEndDate.Value) : new();
            Model.EndTime = _selectedEndTime.HasValue ? TimeOnly.FromTimeSpan(_selectedEndTime.Value) : new();
            Model.SchedulePaperSelectedFormsIds = _selectedForms.ConvertAll(x => x.Id);

            var response = await BlazSchedulePaperService.UpdateSchedulePaperAsync(Model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _isSubmitButtonHit = false;

                Snackbar.Add(response.Message, Severity.Success);

                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }
        else
        {
            Snackbar.Add(Resource.PleaseFillRequiredFieldsCorrectly, Severity.Error);
        }

        StateHasChanged();
    }

    private void OnCancelButtonHit()
    {
        MudDialog.Cancel();
    }


    // HELPER METHODS

    private bool ValidateSchedulePaperFormArguments()
    {
        return _selectedPaper?.Id > 0 &&
               !string.IsNullOrWhiteSpace(Model.Description) &&
               _selectedStartDate != null &&
               _selectedEndDate != null &&
               _selectedStartTime != null &&
               _selectedEndTime != null &&
               _selectedForms.Count != 0;
    }

    private bool ValidateStartAndEndDateTimes()
    {
        if (_selectedStartDate.HasValue &&
            _selectedEndDate.HasValue &&
            _selectedStartTime.HasValue &&
            _selectedEndTime.HasValue)
        {
            var startDateTime = _selectedStartDate.Value.Date.Add(_selectedStartTime.Value);
            var endDateTime = _selectedEndDate.Value.Date.Add(_selectedEndTime.Value);

            if (endDateTime <= startDateTime)
            {
                Snackbar.Add(Resource.EndDateTimeMustBeAfterStartDateTime, Severity.Error);
                return false;
            }

            return ValidateAgainstScheduleTimeBoundaries();
        }

        return false;
    }

    private bool ValidateSamePaperDateTimeIntersections()
    {
        DateTime selectedStartDateTime = _selectedStartDate.Value.Date.Add(_selectedStartTime.Value);
        DateTime selectedEndDateTime = _selectedEndDate.Value.Date.Add(_selectedEndTime.Value);

        var intersectingPaperInDateTime = CurrentSchedulePapersList.Find(p =>
        {
            if (p.PaperId == _selectedPaper.Id)
            {
                if (p.Id == SchedulePaperId) return false;

                DateTime paperStartDateTime = p.StartDate.ToDateTime(p.StartTime);
                DateTime paperEndDateTime = p.EndDate.ToDateTime(p.EndTime);

                // Checks for overlapping here:
                return paperStartDateTime <= selectedEndDateTime && paperEndDateTime >= selectedStartDateTime;
            }

            return false;
        });

        if (intersectingPaperInDateTime != null)
        {
            Snackbar.Add(string.Format(Resource.PaperOverlapErrorFormat,
                                       intersectingPaperInDateTime.PaperName,
                                       intersectingPaperInDateTime.SessionDescription),
                         Severity.Error);
            return false;
        }

        return true;
    }

    private bool ValidateAgainstScheduleTimeBoundaries()
    {
        List<string> errorMessages = [];

        var paperStartDateTime = _selectedStartDate.Value.Date.Add(_selectedStartTime.Value);
        var paperEndDateTime = _selectedEndDate.Value.Date.Add(_selectedEndTime.Value);

        var scheduleStartDateTime = ScheduleMetadataResultedParamsDto.StartDate.ToDateTime(ScheduleMetadataResultedParamsDto.StartTime);
        var scheduleEndDateTime = ScheduleMetadataResultedParamsDto.EndDate.ToDateTime(ScheduleMetadataResultedParamsDto.EndTime);

        if (paperStartDateTime < scheduleStartDateTime)
        {
            var message = string.Format(
                Resource.PaperCannotStartMessage,
                scheduleStartDateTime.ToShortDateString(),
                scheduleStartDateTime.ToShortTimeString(),
                scheduleEndDateTime.ToShortDateString(),
                scheduleEndDateTime.ToShortTimeString()
            );

            errorMessages.Add(message);
        }

        if (paperEndDateTime > scheduleEndDateTime)
        {
            var message = string.Format(
                Resource.PaperCannotEndMessage,
                scheduleEndDateTime.ToShortDateString(),
                scheduleEndDateTime.ToShortTimeString()
            );

            errorMessages.Add(message);
        }

        errorMessages.ForEach(error => Snackbar.Add(error, Severity.Error));

        return errorMessages.Count == 0;
    }


    // DATE & TIME CHANGE HANDLERS

    private void OnStartDateChanged(DateTime? newDate) => HandleDateChanged(newDate, ref _selectedStartDate, ref _selectedStartTime);

    private void OnEndDateChanged(DateTime? newDate) => HandleDateChanged(newDate, ref _selectedEndDate, ref _selectedEndTime);

    private void OnStartTimeChanged(TimeSpan? newTime) => HandleTimeChanged(newTime, _selectedStartDate, ref _selectedStartTime);

    private void OnEndTimeChanged(TimeSpan? newTime) => HandleTimeChanged(newTime, _selectedEndDate, ref _selectedEndTime);

    private void HandleDateChanged(DateTime? newDate, ref DateTime? selectedDate, ref TimeSpan? selectedTime)
    {
        selectedDate = newDate;

        if (selectedTime.HasValue && newDate.HasValue)
        {
            selectedTime = ClampTime(selectedTime.Value, DateOnly.FromDateTime(newDate.Value));
        }
    }

    private void HandleTimeChanged(TimeSpan? newTime, DateTime? selectedDate, ref TimeSpan? selectedTime)
    {
        if (newTime.HasValue && selectedDate.HasValue)
        {
            var clampedTime = ClampTime(newTime.Value, DateOnly.FromDateTime(selectedDate.Value));

            if (clampedTime != newTime.Value)
            {
                Snackbar.Add(string.Format(Resource.TimeAdjustedToScheduleBoundary,
                    TimeOnly.FromTimeSpan(clampedTime).ToString("hh:mm tt")), Severity.Warning);
            }

            selectedTime = clampedTime;
        }
        else
        {
            selectedTime = newTime;
        }
    }

    private TimeSpan ClampTime(TimeSpan time, DateOnly date)
    {
        if (date == ScheduleMetadataResultedParamsDto.StartDate && time < ScheduleMetadataResultedParamsDto.StartTime.ToTimeSpan())
            return ScheduleMetadataResultedParamsDto.StartTime.ToTimeSpan();

        if (date == ScheduleMetadataResultedParamsDto.EndDate && time > ScheduleMetadataResultedParamsDto.EndTime.ToTimeSpan())
            return ScheduleMetadataResultedParamsDto.EndTime.ToTimeSpan();

        return time;
    }
}