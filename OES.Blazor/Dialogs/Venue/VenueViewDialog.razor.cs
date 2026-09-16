using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.General;
using System.Net;

namespace OES.Blazor.Dialogs.Venue
{
    public partial class VenueViewDialog
    {
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] IBlazVenueService BlazVenueService { get; set; }
        [Inject] IDialogService DialogService { get; set; }

        [Parameter] public VenueViewDto Model { get; set; } = new();

        private bool IsInitialized = false;


        protected override async Task OnInitializedAsync()
        {
            var id = await BlazSessionStorageService.GetValue<long>(MiscConstants.PerformViewBtnClick);

            if (id <= 0)
            {
                IsInitialized = true;

                return;
            }

            var response = await BlazVenueService.GetVenueById(id);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseModel = response.Data as EditVenueRequestDto;
                Model.Code = responseModel.Code;
                Model.Name = responseModel.Name;
                Model.DisplayName = responseModel.DisplayName;
                Model.Address = responseModel.Address;
                Model.GeoLocation = responseModel.GeoLocation;
                Model.PinCode = responseModel.PinCode;
                Model.VenueEmail = responseModel.VenueEmail;
                Model.Mobile = responseModel.Mobile;
                Model.CoordinatorFullName = responseModel.CoordinatorFullName;
                Model.CoordinatorEmail = responseModel.CoordinatorEmail;
                Model.CoordinatorMobile = responseModel.CoordinatorMobile;
                Model.IPAddress = responseModel.IPAddress;
                Model.Url = responseModel.Url;
                Model.TCIds = responseModel.TCIds;
            }
            else
            {
                Model = new VenueViewDto();
                await DialogService.ShowMessageBox("Error", response.Message, yesText: "OK");
            }

            IsInitialized = true;
        }
    }
}
