using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.Paper.Responses;

namespace OES.Blazor.Pages.Paper.Main.PaperViewDetails
{
    public partial class PaperViewDetails : ComponentBase
    {
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazPaperService _BlazPaperService { get; set; }

        public PaperDataViewResponseDto paperDetails { get; set; } = new();

        private long paperId { get; set; }

        private bool _isDataLoaded = false;

        protected override async Task OnInitializedAsync()
        {
            paperId = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");

            paperDetails = await _BlazPaperService.GetPaperDataForViewByIdAsync(paperId);

            _isDataLoaded = true;

            StateHasChanged();
        }

        void Close() => NavigationManager.NavigateTo("/UserPapers");
    }
}
