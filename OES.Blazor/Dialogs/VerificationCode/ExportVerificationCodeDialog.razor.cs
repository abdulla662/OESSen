using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Text.Json;

namespace OES.Blazor.Dialogs.VerificationCode
{
    public partial class ExportVerificationCodeDialog
    {
        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;

        [Inject] private IJSRuntime JS { get; set; } = null!;

        [Inject] private IBlazAuthService BlazAuthService { get; set; } = null!;

        [Inject] private IBlazVenueService VenueService { get; set; } = null!;

        [Inject] private ISnackbar Snackbar { get; set; } = null!;

        private List<VenueLookupDto> _venues = [];

        private IEnumerable<VenueLookupDto> _selectedVenues = [];

        private ExamDateFilter _selectedExamDate = ExamDateFilter.Today;

        private bool _isExporting;

        protected override async Task OnInitializedAsync()
        {
            _venues = await VenueService.GetVenueLookupAsync();
        }

        private void Cancel() => MudDialog.Close();

        private async Task ExportAsync()
        {
            if (_selectedVenues == null || !_selectedVenues.Any())
            {
                Snackbar.Add(Resource.PleaseSelectVenueFirst, Severity.Warning);
                return;
            }

            _isExporting = true;

            try
            {
                var token = await BlazAuthService.GetDecryptedTokenFromLocalStorageAsync();
                var url = $"{CentralizedUrlHelper.OesApiBaseUrl}api/Candidate/ExportVerificationCodeExcel";

                var body = new ExportVerificationCodeRequestDto
                {
                    VenueIds = _selectedVenues.Select(v => v.Id).ToList(),
                    ExamDate = _selectedExamDate
                };

                var bodyJson = JsonSerializer.Serialize(body);
                var fileName = $"VerificationCodes_{DateTime.Now:yyyy-MM-dd}.xlsx";

                await JS.InvokeVoidAsync("downloadExcelViaFetch", url, bodyJson, fileName, token);
            }
            finally
            {
                _isExporting = false;
            }
        }
    }
}
