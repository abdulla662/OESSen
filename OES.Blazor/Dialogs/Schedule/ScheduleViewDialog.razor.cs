using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Blazor.Services.Interfaces.ScheduleSecurityConfigurationsService;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Net;

namespace OES.Blazor.Dialogs.Schedule;

public partial class ScheduleViewDialog : ComponentBase
{
    [Inject] private IBlazQuestionLanguageService BlazLanguageService { get; set; }

    [Inject] private IBlazSecurityConfigurationsService BlazSecurityConfigurationsService { get; set; }

    [Inject] private IJSRuntime JS { get; set; }

    [Inject] private IBlazVenueService BlazVenueService { get; set; }

    [Inject] private IBlazScheduleService BlazScheduleService { get; set; }

    [Inject] private ISnackbar Snackbar { get; set; }

    [Inject] private IBlazSchedulePaperService BlazSchedulePaperService { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private IBlazSessionStorageService SessoinStorage { set; get; }

    [Inject] private IDialogService DialogService { get; set; }

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;


    private List<LanguageDto> Languages = [];

    private List<GetVenueResponseDto> Venues = [];

    private ScheduleSecConfigResponseDto SecurityTemplate = new();

    private ScheduleMetadataDto Model = new();

    private List<SchedulePaperPaginationDto> Papers = [];


    protected override async Task OnInitializedAsync()
    {
        var id = await SessoinStorage.GetValue<long>("PerformViewBtnClick");

        var languagesTask = BlazLanguageService.GetAllLanguagesAsync();
        var venuesTask = BlazVenueService.GetAllVenuesAsync();
        var securityTemplateTask = BlazSecurityConfigurationsService.GetSecurityConfigurationsTemplateByIdAsync(id);

        await Task.WhenAll(languagesTask, venuesTask, securityTemplateTask);

        Languages = await languagesTask;
        Venues = await venuesTask;
        var securityTemplateResponse = await securityTemplateTask;

        if (securityTemplateResponse?.StatusCode == HttpStatusCode.OK && securityTemplateResponse.Data != null)
        {
            SecurityTemplate = securityTemplateResponse.Data as ScheduleSecConfigResponseDto;
        }

        var fetchedSchedueledMetaDataDto = await BlazScheduleService.GetScheduleByIdAsync(id);

        if (fetchedSchedueledMetaDataDto is null)
        {
            Snackbar.Add(Resource.ScheduleNotFound, Severity.Error);
            NavigationManager.NavigateTo("/Schedules");
            return;
        }

        Model = fetchedSchedueledMetaDataDto;

        var pagination = new PaginationSearchModel { PaginationOff = true };

        var tableData = await BlazSchedulePaperService.GetAllSchedulePapersByScheduleIdAsync(pagination, Model.Id);

        Papers = tableData?.Items?.ToList() ?? [];
    }

    private string GetLanguageDisplayText()
    {
        var selected = Languages.Where(l => Model.LanguageIds.Contains(l.Id)).Select(l => l.Name).ToList();

        if (selected.Count == 1)
            return selected[0];

        return string.Join(", ", selected);
    }

    private string GetVenuesDisplayText()
    {
        var selected = Venues.Where(l => Model.ExamVenueIds.Contains(l.Id)).Select(l => l.Name).ToList();

        if (selected.Count == 1)
            return selected[0];

        return string.Join(", ", selected);
    }

    private async Task ViewSecurityConfigurationAsync()
    {
        if (SecurityTemplate == null)
        {
            Snackbar.Add(Resource.SecurityTemplateLoadFailed, Severity.Error);
            return;
        }

        await ViewTemplateAsync("/SecurityConfigurationsTemplateView");
    }

    private async Task ViewPaperSettingAsync(long schedulePaperId)
    {
        await SessoinStorage.SetValue("SchedulePaperId", schedulePaperId);

        await ViewTemplateAsync("/PaperSettingTemplateView");
    }

    private async Task DownloadCandidatesExcelAsync(long schedulePaperId)
    {
        await BlazSchedulePaperService.ExportCandidatesToExcelAsync(schedulePaperId);
    }

    private async Task ViewTemplateAsync(string url)
    {
        await JS.InvokeVoidAsync("openInNewTab", url);
    }
}