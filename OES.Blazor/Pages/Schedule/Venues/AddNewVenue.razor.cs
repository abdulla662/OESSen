using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.RegularExpressions;
using RegexPatterns = OES.Helper.RegularExpressions.RegularExpressions;

namespace OES.Blazor.Pages.Schedule.Venues
{
    public partial class AddNewVenue : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] public IBlazVenueService VenueService { get; set; }

        private string _newTCCode = string.Empty;
        private readonly List<string> _tcCodesList = [];
        private InputType passwordInputType = InputType.Password;
        private bool _isSubmitting = false;

        private AddVenueRequestDto Model { get; set; } = new();
        private bool IsValidTCCode => !string.IsNullOrWhiteSpace(_newTCCode) && _newTCCode.Trim().All(char.IsDigit);
        private bool HasTCCodeError => !string.IsNullOrWhiteSpace(_newTCCode) && !_newTCCode.Trim().All(char.IsDigit);
        private string TCCodeErrorText => HasTCCodeError ? Resource.TCCodeMustBeNumeric : string.Empty;
        private bool HasUrlError => !string.IsNullOrWhiteSpace(Model?.Url) && !IsValidUrl(Model.Url);
        private string UrlErrorText => HasUrlError ? Resource.UrlInvalid : string.Empty;
        private string PasswordInputIcon =>
            passwordInputType == InputType.Password ?
            Icons.Material.Filled.VisibilityOff :
            Icons.Material.Filled.Visibility;
        private bool IsSubmitButtonDisabled =>
            _isSubmitting ||
            string.IsNullOrWhiteSpace(Model?.Name) ||
            string.IsNullOrWhiteSpace(Model?.Code) ||
            string.IsNullOrWhiteSpace(Model?.Address) ||
            string.IsNullOrWhiteSpace(Model?.PinCode) ||
            string.IsNullOrWhiteSpace(Model?.Mobile) ||
            string.IsNullOrWhiteSpace(Model?.IPAddress) ||
            HasUrlError ||
            string.IsNullOrWhiteSpace(Model?.CoordinatorFullName) ||
            string.IsNullOrWhiteSpace(Model?.CoordinatorVenuePassword) ||
            string.IsNullOrWhiteSpace(Model?.VenueEmail) ||
            string.IsNullOrWhiteSpace(Model?.CoordinatorEmail) ||
            string.IsNullOrWhiteSpace(Model?.CoordinatorMobile) ||
            _tcCodesList.Count == 0;

        private async Task OnSubmit()
        {
            if (_isSubmitting) return;

            _isSubmitting = true;

            var response = await VenueService.AddVenueAsync(Model);

            _isSubmitting = false;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.VenueCreatedSuccefully, Severity.Success);
                Navigation.NavigateTo("/Venues");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void NavigateBack()
        {
            Navigation.NavigateTo("/Venues");
        }

        private void TogglePasswordVisibility()
        {
            passwordInputType = passwordInputType == InputType.Password
                ? InputType.Text
                : InputType.Password;
        }

        private void SyncTCIdsToModel()
        {
            Model.TCIds = string.Join(",", _tcCodesList);
        }

        private void AddTCCode()
        {
            var code = _newTCCode?.Trim();

            if (_tcCodesList.Contains(code))
            {
                Snackbar.Add(Resource.TCCodeAlreadyExists, Severity.Warning);
                _newTCCode = string.Empty;
                return;
            }

            _tcCodesList.Add(code);
            SyncTCIdsToModel();
            _newTCCode = string.Empty;
        }

        private void RemoveTCCode(string code)
        {
            _tcCodesList.Remove(code);
            SyncTCIdsToModel();
        }

        private static bool IsValidUrl(string value) => !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), RegexPatterns.UrlCheck);
    }
}