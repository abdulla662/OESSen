using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper.Transition;
using OES.Helper.Dtos.Paper.TransitionDtos.Request;

namespace OES.Blazor.Pages.Paper.TransitionLevel
{
    public partial class TransitionLevelDetails : ComponentBase
    {
        [Inject] IBlazTransitionLevelService BlazTransitionLevelService { get; set; }
        [Inject] IBlazSessionStorageService SessionStorage { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        GetTransitionLevelDto model = new();

        protected override async Task OnInitializedAsync()
        {
            var id = await SessionStorage.GetValue<long>("PerformViewBtnClick");

            model = await BlazTransitionLevelService.GetTransitionLevelById(id);
        }

        private void Close() => NavigationManager.NavigateTo("/TransitionLevels");
    }
}
