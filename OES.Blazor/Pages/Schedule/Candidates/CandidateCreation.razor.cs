using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.Disability;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.RegularExpressions;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.Schedule.Candidates
{
    public partial class CandidateCreation
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; }
        [Inject] private IBlazDisabilityService DisabilityService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }


        private AddOrUpdateCandidateRequestDto _candidateDto = new();

        private const long MaxAllowedSize = 5 * 1024 * 1024;

        private InputType PasswordInput = InputType.Password;

        private string PasswordInputIcon = Icons.Material.Filled.VisibilityOff;

        private string _errorMessage = string.Empty;

        private string _successMessage = string.Empty;

        private int _photoKey = 0;

        private int _signatureKey = 0;

        private bool _showed = false;

        private IBrowserFile _photoFile;

        private IBrowserFile _signatureFile;

        List<AddOrUpdateDisabilityDto> disabilities = [];

        private bool IsFormValid =>
                _photoFile != null &&
                _signatureFile != null &&
                !string.IsNullOrWhiteSpace(_candidateDto.Name) &&
                !string.IsNullOrWhiteSpace(_candidateDto.Password) &&
                !string.IsNullOrWhiteSpace(_candidateDto.CandidateCode) &&
                !string.IsNullOrWhiteSpace(_candidateDto.UserName) &&
                _candidateDto.DateOfBirth != null &&
                (_candidateDto.Gender == 0 || _candidateDto.Gender == 1) &&
                IsValidMobile() &&
                IsValidNationalId() &&
                IsValidEmail();

        private bool IsValidMobile() =>
            !string.IsNullOrWhiteSpace(_candidateDto.Mobile) &&
            Regex.IsMatch(_candidateDto.Mobile, RegularExpressions.InternationalPhoneNumber);

        private bool IsValidNationalId() =>
            !string.IsNullOrWhiteSpace(_candidateDto.NationalId) &&
            Regex.IsMatch(_candidateDto.NationalId, RegularExpressions.NationalNumber);

        private bool IsValidEmail() =>
            !string.IsNullOrWhiteSpace(_candidateDto.Email) &&
            Regex.IsMatch(_candidateDto.Email, RegularExpressions.EmailExpression);

        protected override async Task OnInitializedAsync()
        {
            disabilities = await DisabilityService.GetAllDisabilities();
        }

        private void UploadFilePhoto(IBrowserFile file)
        {
            if (_photoFile != null)
            {
                Snackbar.Add(Resource.OnlyOneImage, Severity.Error);
                return;
            }

            if (file.Size > MaxAllowedSize)
            {
                Snackbar.Add(string.Format(Resource.PhotoFileSize, MaxAllowedSize / (1024.0 * 1024.0)), Severity.Error);
                return;
            }

            _photoFile = file;
            _candidateDto.PhotoFile = file;
        }

        private void UploadFileSignature(IBrowserFile file)
        {
            if (_signatureFile != null)
            {
                Snackbar.Add(Resource.OnlyOneImage, Severity.Error);
                return;
            }

            if (file.Size > MaxAllowedSize)
            {
                Snackbar.Add(string.Format(Resource.PhotoFileSize, MaxAllowedSize / (1024.0 * 1024.0)), Severity.Error);
                return;
            }

            _signatureFile = file;
            _candidateDto.SignatureFile = file;
        }

        private void RemovePhotoFile()
        {
            _photoFile = null;

            _candidateDto.PhotoFile = null;

            _photoKey++;

            StateHasChanged();
        }

        private void RemoveSignatureFile()
        {
            _signatureFile = null;

            _candidateDto.SignatureFile = null;

            _signatureKey++;

            StateHasChanged();
        }

        private void TogglePasswordVisibility()
        {
            _showed = !_showed;

            PasswordInputIcon = _showed ? Icons.Material.Filled.Visibility : Icons.Material.Filled.VisibilityOff;

            PasswordInput = _showed ? InputType.Text : InputType.Password;

            StateHasChanged();
        }

        private async Task HandleValidSubmitAsync()
        {
            if (_candidateDto == null)
            {
                _errorMessage = Resource.CandidateDataIsNotInitialized;

                Snackbar.Add(_errorMessage, Severity.Error);

                return;
            }

            StateHasChanged();

            if (BlazCandidateService == null)
            {
                _errorMessage = Resource.ServiceIsNotInitialized;

                Snackbar.Add(_errorMessage, Severity.Error);

                return;
            }

            var response = await BlazCandidateService.AddCandidateAsync(_candidateDto, _photoFile, _signatureFile);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _successMessage = response.Message;

                Snackbar.Add(_successMessage, Severity.Success);

                NavigationManager.NavigateTo("/Candidates");
            }
            else
            {
                _errorMessage = response.Message;

                Snackbar.Add(_errorMessage, Severity.Error);
            }
        }

        private void Close()
        {
            NavigationManager.NavigateTo("/Candidates");

            StateHasChanged();
        }
    }
}