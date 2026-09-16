using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Report;
using OES.Helper.Dtos.Reports.Analytical;

namespace OES.Blazor.Components.Reports
{
    public partial class AnalyticalReportFilter
    {
        [Inject] private IBlazAnalyticalReportsService AnalyticalReportsService { get; set; } = default!;

        [Parameter] public bool ShowVenue { get; set; } = true;
        [Parameter] public bool ShowPaper { get; set; } = true;
        [Parameter] public bool ShowItemBank { get; set; }
        [Parameter] public bool ShowForm { get; set; }
        [Parameter] public bool ShowDateRange { get; set; }
        [Parameter] public EventCallback<AnalyticalReportFilterDto> FilterChanged { get; set; }
        [Parameter] public EventCallback OnGenerateClicked { get; set; }

        private DateRange _dateRange = new();
        private List<ScheduleLookupDto> _schedules = [];
        private List<VenueLookupDto> _venues = [];
        private List<PaperCodeLookupDto> _papers = [];
        private List<ItemBankLookupDto> _itemBanks = [];
        private List<PaperFormLookupDto> _forms = [];
        private IEnumerable<ScheduleLookupDto> _selectedSchedules = [];
        private IEnumerable<VenueLookupDto> _selectedVenues = [];
        private IEnumerable<PaperCodeLookupDto> _selectedPapers = [];
        private ItemBankLookupDto? _selectedItemBank;
        private IEnumerable<PaperFormLookupDto> _selectedForms = [];

        private bool HasSchedules => _selectedSchedules.Any();
        private bool HasPapers => _selectedPapers.Any();
        private bool CanGenerate => HasSchedules && (!ShowPaper || HasPapers) && (!ShowForm || _selectedForms.Any());

        protected override async Task OnInitializedAsync()
        {
            _schedules = await AnalyticalReportsService.GetSchedulesAsync();

            if (ShowItemBank)
            {
                _itemBanks = await AnalyticalReportsService.GetItemBanksAsync();
            }
        }

        private async Task OnSchedulesChangedAsync(IEnumerable<ScheduleLookupDto> schedules)
        {
            _selectedSchedules = schedules;

            ResetVenueState();
            ResetPaperState();
            ResetFormState();

            var scheduleIds = schedules.Select(x => x.Id).ToList();

            if (scheduleIds.Count > 0)
            {
                await LoadDependentDataAsync(scheduleIds);
            }

            await NotifyChangedAsync();
        }

        private async Task OnVenuesChangedAsync(IEnumerable<VenueLookupDto> venues)
        {
            _selectedVenues = venues;

            await NotifyChangedAsync();
        }

        private async Task OnPapersChangedAsync(IEnumerable<PaperCodeLookupDto> papers)
        {
            _selectedPapers = papers;

            ResetFormState();

            if (ShowForm && _selectedSchedules.Any() && papers.Any())
            {
                _forms = await AnalyticalReportsService.GetFormsAsync(new PaperFormsRequestDto(
                    [.. _selectedSchedules.Select(x => x.Id)],
                    [.. papers.Select(x => x.Code)])
                );
            }

            await NotifyChangedAsync();
        }

        private async Task OnFormsChangedAsync(IEnumerable<PaperFormLookupDto> forms)
        {
            _selectedForms = forms;

            await NotifyChangedAsync();
        }

        private async Task OnItemBankChangedAsync(ItemBankLookupDto bank)
        {
            _selectedItemBank = bank;

            await NotifyChangedAsync();
        }

        private async Task OnDateRangeChangedAsync(DateRange range)
        {
            _dateRange = range;

            await NotifyChangedAsync();
        }

        private async Task LoadDependentDataAsync(List<long> scheduleIds)
        {
            if (ShowVenue)
            {
                _venues = await AnalyticalReportsService.GetVenuesAsync(scheduleIds);
            }

            if (ShowPaper)
            {
                _papers = await AnalyticalReportsService.GetPapersAsync(scheduleIds);
            }
        }

        private void ResetVenueState()
        {
            _selectedVenues = [];
            _venues = [];
        }

        private void ResetPaperState()
        {
            _selectedPapers = [];
            _papers = [];
        }

        private void ResetFormState()
        {
            _selectedForms = [];
            _forms = [];
        }

        private async Task NotifyChangedAsync()
        {
            await FilterChanged.InvokeAsync(new AnalyticalReportFilterDto
            {
                ScheduleIds = [.. _selectedSchedules.Select(x => x.Id)],
                VenueCodes = [.. _selectedVenues.Select(x => x.Code)],
                PaperCodes = [.. _selectedPapers.Select(x => x.Code)],

                FormIds = [.. _selectedForms.Select(x => x.Id)],
                DateFrom = _dateRange.Start,
                DateTo = _dateRange.End
            });
        }
    }
}
