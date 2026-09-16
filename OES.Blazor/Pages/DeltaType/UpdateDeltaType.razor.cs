using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.DeltaType;
using System.Net;

namespace OES.Blazor.Pages.DeltaType
{
    public partial class UpdateDeltaType : ComponentBase
    {
        [Inject] IBlazDeltaTypeService BlazDeltaTypeService { get; set; }
        [Inject] IBlazSessionStorageService SessionStorageService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private GetDeltaTypeDto model = new();

        protected override async Task OnInitializedAsync()
        {
            var id = await SessionStorageService.GetValue<long>("PerformEditBtnClick");

            model = await BlazDeltaTypeService.GetDeltaTypeById(id);
        }

        private async Task OnValidSubmitAsync()
        {
            var response = await BlazDeltaTypeService.UpdateDeltaType(model);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);

                NavigationManager.NavigateTo("/DeltaTypeList");
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Cancel() => NavigationManager.NavigateTo("/DeltaTypeList");
    }
}
