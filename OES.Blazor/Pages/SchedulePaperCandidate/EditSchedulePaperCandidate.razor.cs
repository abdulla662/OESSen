using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.SchedulePaperCandidate
{
    public partial class EditSchedulePaperCandidate
    {
        [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; } = default!;
        [Inject] private IBlazVenueService BlazVenueService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private string _registrationNumber = string.Empty;
        private bool _isLoading = false;
        private bool _isSaving = false;
        private bool _notFound = false;
        private SchedulePaperCandidateDetailsDto _candidate = null;
        private bool _isSubmitButtonHit;
        private DateTime? _selectedExamDate;
        private TimeSpan? _selectedExamTime;
        private string _newCenterCode = string.Empty;
        private List<GetVenueResponseDto> _venues = [];
        private GetVenueResponseDto _selectedVenue = null;

        private DateTime? NewExamDateTime =>
            _selectedExamDate.HasValue && _selectedExamTime.HasValue
                ? _selectedExamDate.Value.Date.Add(_selectedExamTime.Value)
                : null;

        private List<string> CenterCodes =>
            (_selectedVenue?.TCIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        protected override async Task OnInitializedAsync()
        {
            _venues = await BlazVenueService.GetAllVenuesAsync() ?? [];
        }

        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(_registrationNumber))
            {
                Snackbar.Add(@Resource.PleaseEnterARegistrationId, Severity.Warning);
                return;
            }

            if (!long.TryParse(_registrationNumber, out var regNumber))
            {
                Snackbar.Add(@Resource.RegistrationIdMustBeANumber, Severity.Warning);
                return;
            }

            _isLoading = true;
            _notFound = false;
            _candidate = null;
            StateHasChanged();

            _candidate = await BlazSchedulePaperService.GetSchedulePaperCandidateByRegistrationNumberAsync(regNumber);

            if (_candidate == null)
                _notFound = true;

            ClearEditForm();

            _isLoading = false;
            StateHasChanged();
        }

        private async Task SaveChangesAsync()
        {
            if (_candidate == null) return;

            _isSubmitButtonHit = true;

            if (!ValidateRescheduleArguments())
                return;

            if (IsSameAsCurrentDetails())
            {
                Snackbar.Add(Resource.NothingToUpdateTheExamDateVenueAndCenterCodeYouSelectedAreTheCandidatesCurrentDetails, Severity.Warning);
                return;
            }

            _isSaving = true;
            StateHasChanged();

            var request = new UpdateSchedulePaperCandidateRequestDto
            {
                SchedulePaperCandidateId = _candidate.Id,
                NewExamDate = NewExamDateTime,
                NewVenueId = _selectedVenue.Id,
                NewVenueCode = _selectedVenue.Code,
                NewCenterCode = _newCenterCode,
                RegistrationNumber = _candidate.RegistrationNumber,
                OldVenueCode = _candidate.VenueCode
            };

            var response = await BlazSchedulePaperService.UpdateSchedulePaperCandidateAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, response.CustomCodeStatus == CustomCodeStatus.Success ? Severity.Success : Severity.Warning);

                await SearchAsync();
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            _isSaving = false;
            StateHasChanged();
        }

        private void Cancel()
        {
            ClearEditForm();
            StateHasChanged();
        }

        private Task<IEnumerable<GetVenueResponseDto>> SearchVenuesAsync(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult<IEnumerable<GetVenueResponseDto>>(_venues);

            return Task.FromResult(_venues.Where(v =>
                v.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase) ||
                v.Code.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
        }

        private void OnVenueChanged(GetVenueResponseDto venue)
        {
            _selectedVenue = venue;

            if (!CenterCodes.Contains(_newCenterCode))
                _newCenterCode = string.Empty;
        }

        private bool ValidateRescheduleArguments()
        {
            return _selectedExamDate != null &&
                   _selectedExamTime != null &&
                   _selectedVenue != null &&
                   !string.IsNullOrWhiteSpace(_newCenterCode);
        }

        private bool IsSameAsCurrentDetails()
        {
            return NewExamDateTime == _candidate.CandidateExamDate
                && _selectedVenue?.Id == _candidate.VenueId
                && string.Equals(_newCenterCode?.Trim(), _candidate.CenterCode?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void ClearEditForm()
        {
            _isSubmitButtonHit = false;
            _selectedExamDate = null;
            _selectedExamTime = null;
            _selectedVenue = null;
            _newCenterCode = string.Empty;
        }
    }
}