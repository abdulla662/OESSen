using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.DeltaType;
using OES.Helper.Dtos.DeltaType;
using System.Net;

namespace OES.Blazor.Pages.DeltaType
{
    public partial class AddDeltaType : ComponentBase
    {
        [Inject] IBlazDeltaTypeService BlazDeltaTypeService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] NavigationManager NavigationManager { get; set; }


        private AddDeltaTypeDto model = new();


        private async Task OnValidSubmitAsync()
        {
            var response = await BlazDeltaTypeService.AddDeltaType(model);

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
