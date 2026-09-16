using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.AuditLogs;
using OES.Blazor.Services.Interfaces.AuditLogs;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Helper.Dtos.AuditLogs;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Text.Json;

namespace OES.Blazor.Pages.AuditLogs
{
    public partial class AuditLogsList : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IBlazAuditLogsService BlazAuditLogsService { get; set; }
        [Inject] private IBlazAuthService BlazAuthService { get; set; }
        [Inject] private IJSRuntime JS { get; set; }

        private Guid _logListChangingKey = Guid.NewGuid();
        private PaginationSearchModel _filterObject = new();

        private string _username;
        private string _pathName;
        private string _selectedPageName;
        private List<string> _pageNames = AuditLogPageResolver.GetAllPageNames();
        private DateRange _dateRange = new();
        private bool _isExporting;

        private void ApplyFilters()
        {
            _filterObject = new PaginationSearchModel
            {
                SearchKey = string.IsNullOrWhiteSpace(_username) ? null : _username.Trim(),
                FromDate = _dateRange.Start,
                ToDate = _dateRange.End?.AddDays(1).AddTicks(-1),
                FilterObj = new AuditLogsFilterDto
                {
                    PathName = string.IsNullOrWhiteSpace(_pathName) ? null : _pathName.Trim(),
                    PageName = string.IsNullOrWhiteSpace(_selectedPageName) ? null : _selectedPageName
                }
            };

            _logListChangingKey = Guid.NewGuid();
        }

        private void ResetFilters()
        {
            _username = null;
            _pathName = null;
            _selectedPageName = null;
            _dateRange = new();
            _filterObject = new PaginationSearchModel();
            _logListChangingKey = Guid.NewGuid();
        }

        private async Task ExportToExcelAsync()
        {
            _isExporting = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                var token = await BlazAuthService.GetDecryptedTokenFromLocalStorageAsync();
                var url = $"{CentralizedUrlHelper.OesApiBaseUrl}api/AuditLogs/ExportExcel";
                var bodyJson = JsonSerializer.Serialize(_filterObject);
                var fileName = $"AuditLogs_{DateTime.Now:yyyy-MM-dd}.xlsx";

                await JS.InvokeVoidAsync("downloadExcelViaFetch", url, bodyJson, fileName, token);
            }
            finally
            {
                _isExporting = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private void ViewDetails(GetAuditLogsDto log)
        {
            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true
            };

            var parameters = new DialogParameters
            {
                { nameof(AuditLogsDetailsDialog.Log), log }
            };

            DialogService.Show<AuditLogsDetailsDialog>(Resource.LogDetails, parameters, options);
        }
    }
}
