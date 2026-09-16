using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Disability;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.Enums;
using OES.Helper.General;

namespace OES.Blazor.Pages.Disabilities
{
    public partial class UpdateDisability
    {
        [Inject] IBlazDisabilityService _blazDisabilityService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazSessionStorageService ISessoinStorage { set; get; }

        public ApiResponse MessageResponse { get; set; }
        private AddOrUpdateDisabilityDto Model { get; set; } = new();


        protected override async Task OnInitializedAsync()
        {
            var id = await ISessoinStorage.GetValue<long>("PerformEditBtnClick");
            Model = (AddOrUpdateDisabilityDto)(await _blazDisabilityService.GetDisabilityById(id)).Data;
        }

        public async Task OnValidSubmitAsync()
        {
            MessageResponse = await _blazDisabilityService.UpdateDisability(Model);

            StateHasChanged();

            if (MessageResponse.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(MessageResponse.Message, Severity.Success);
                NavigationManager.NavigateTo("/Disabilities");
            }
            else
            {
                Snackbar.Add(MessageResponse.Message, Severity.Error);
            }
        }

        private void Cancel()
        {
            NavigationManager.NavigateTo("/Disabilities");
        }
    }
}


