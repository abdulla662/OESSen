using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.Disability;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.RegularExpressions;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.Schedule.Candidates
{
    public partial class UpdateCandidate
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; }
        [Inject] private IBlazSessionStorageService SessionStorage { get; set; }
        [Inject] private IBlazDisabilityService DisabilityService { get; set; }

        private IBrowserFile PhotoFile { get; set; }
        private IBrowserFile SignatureFile { get; set; }
        private bool ShowPhotoUrl { get; set; } = false;
        private bool ShowSignatureUrl { get; set; } = false;
        private long CandidateId { get; set; }
        private AddOrUpdateCandidateRequestDto Model { get; set; } = new();
        private InputType PasswordInput { get; set; } = InputType.Password;
        private string PasswordInputIcon { get; set; } = Icons.Material.Filled.VisibilityOff;

        private bool IsFormValid =>
                !string.IsNullOrWhiteSpace(Model.Name) &&
                !string.IsNullOrWhiteSpace(Model.Password) &&
                !string.IsNullOrWhiteSpace(Model.CandidateCode) &&
                !string.IsNullOrWhiteSpace(Model.UserName) &&
                Model.DateOfBirth != null &&
                (Model.Gender == 0 || Model.Gender == 1) &&
                IsValidMobile() &&
                IsValidNationalId() &&
                IsValidEmail();

        private bool IsValidMobile() =>
            !string.IsNullOrWhiteSpace(Model.Mobile) &&
            Regex.IsMatch(Model.Mobile, RegularExpressions.InternationalPhoneNumber);

        private bool IsValidNationalId() =>
            !string.IsNullOrWhiteSpace(Model.NationalId) &&
            Regex.IsMatch(Model.NationalId, RegularExpressions.NationalNumber);

        private bool IsValidEmail() =>
            !string.IsNullOrWhiteSpace(Model.Email) &&
            Regex.IsMatch(Model.Email, RegularExpressions.EmailExpression);


        private const long MaxAllowedSize = 5 * 1024 * 1024;

        private List<AddOrUpdateDisabilityDto> disabilities = [];
        private bool _showed = false;

        protected override async Task OnInitializedAsync()
        {
            CandidateId = await SessionStorage.GetValue<long>("PerformEditBtnClick");
            Model = await BlazCandidateService.GetCandidateByIdAsync(CandidateId);
            disabilities = await DisabilityService.GetAllDisabilities() ?? [];

            StateHasChanged();
        }

        private async Task HandleValidSubmitAsync()
        {
            Model.CandidateId = CandidateId;

            var response = await BlazCandidateService.UpdateCandidateAsync(Model, PhotoFile, SignatureFile);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/Candidates");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void UploadFilePhoto(IBrowserFile file)
        {
            if (file.Size > MaxAllowedSize)
            {
                Snackbar.Add(string.Format(Resource.PhotoFileSize, MaxAllowedSize / (1024.0 * 1024.0)), Severity.Error);

                return;
            }

            PhotoFile = file;

            StateHasChanged();
        }

        private void UploadFileSignature(IBrowserFile file)
        {
            if (file.Size > MaxAllowedSize)
            {
                Snackbar.Add(string.Format(Resource.PhotoFileSize, MaxAllowedSize / (1024.0 * 1024.0)), Severity.Error);

                return;
            }

            SignatureFile = file;

            StateHasChanged();
        }

        private void RemovePhotoFile()
        {
            PhotoFile = null;

            StateHasChanged();
        }

        private void RemoveSignatureFile()
        {
            SignatureFile = null;

            StateHasChanged();
        }

        private void TogglePasswordVisibility()
        {
            _showed = !_showed;

            PasswordInputIcon = _showed ? Icons.Material.Filled.Visibility : Icons.Material.Filled.VisibilityOff;

            PasswordInput = _showed ? InputType.Text : InputType.Password;

            StateHasChanged();
        }

        private void TogglePhotoUrlVisibility()
        {
            ShowPhotoUrl = !ShowPhotoUrl;

            StateHasChanged();
        }

        private void ToggleSignatureUrlVisibility()
        {
            ShowSignatureUrl = !ShowSignatureUrl;

            StateHasChanged();
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/Candidates");
        }
    }
}