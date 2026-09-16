using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces;
using OES.Helper.Dtos.DashbBoard;
using OES.Helper.General.GlobalUserContext;

namespace OES.Blazor.Pages.Dashboard
{
    public partial class Dashboard : ComponentBase
    {
        [Inject] public IBlazDashboardService DashboardService { get; set; } = default!;

        [Inject] public NavigationManager NavigationManager { get; set; } = default!;

        [Inject] public GlobalUserContext GlobalUserContext { get; set; } = default!;


        private DashboardStatisticsDto dashboardStats;


        protected override async Task OnInitializedAsync()
        {
            dashboardStats = await DashboardService.GetDashboardStatisticsAsync();
        }

        private static string FormatNumber(int number)
        {
            var isArabic = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            if (isArabic)
            {
                return number.ToString();
            }

            if (number >= 1_000_000)
                return (number / 1_000_000D).ToString("0.#") + "M";

            if (number >= 1_000)
                return (number / 1_000D).ToString("0.#") + "K";

            return number.ToString();
        }

        private void NavToItemBank()
        {
            NavigationManager.NavigateTo("/ItemBankList");
        }

        private void NavToIlo()
        {
            NavigationManager.NavigateTo("/ILORoots");
        }

        private void NavToQuestions()
        {
            NavigationManager.NavigateTo("/Questions");
        }

        private void NavToUserPapers()
        {
            NavigationManager.NavigateTo("/UserPapers");
        }

        private void NavToSchedules()
        {
            NavigationManager.NavigateTo("/Schedules");
        }

        private void NavToFileManager()
        {
            NavigationManager.NavigateTo("/FileManager");
        }

        private void NavToCandidates()
        {
            NavigationManager.NavigateTo("/Candidates");
        }
    }
}

