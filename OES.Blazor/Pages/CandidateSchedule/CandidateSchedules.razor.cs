using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.CandidateSchedule
{
    public partial class CandidateSchedules
    {
        [Inject] private IBlazScheduleService BlazScheduleService { get; set; } = default!;

        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IBlazAuthService AuthService { get; set; } = default!;

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; } = default!;

        private async Task<CustomTableData<ScheduleMetadataPaginationDto>> GetPublishedSchedulesAsync(PaginationSearchModel paginationSearchModel)
        {
            var allUnexpiredPublishedSchedulesPaginated = await BlazScheduleService.GetAllUnexpiredPublishedSchedulesPaginatedAsync(paginationSearchModel);

            return allUnexpiredPublishedSchedulesPaginated;
        }

        private async Task ViewScheduleAsync(long scheduleId)
        {
            var authorized = await AuthService.IsCurrentUserInRoleAsync(OesTemplateRoleConstants.ScheduleViewer);

            if (!authorized)
            {
                Snackbar.Add(Resource.NotAuthorized, Severity.Error);
                return;
            }

            await BlazSessionStorageService.SetCrudSessionAsync(
                scheduleId,
                MiscConstants.PerformViewBtnClick);

            NavigationManager.NavigateTo("/CandidateSchedulePapersList");
        }
    }
}
