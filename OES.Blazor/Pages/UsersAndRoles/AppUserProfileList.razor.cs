using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.User;
using OES.Blazor.Services.Interfaces.UserService;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.UsersAndRoles
{
    public partial class AppUserProfileList : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;


        [Inject] IBlazUserService BlazUserService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] GlobalUserContext _globalUserContext { get; set; } = default!;

        private bool SyncDisabled { get; set; } = false;
        private int SyncData;

        //private async Task UpdateProfileAsync(object id)
        //{
        //    var parsedId = Guid.Parse(id.ToString()!);

        //    var userProfileDetailsDto = await BlazUserProfileService.GetUserProfileDetailsAsync(parsedId);

        //    var options = new DialogOptions
        //    {
        //        CloseOnEscapeKey = true,
        //        FullWidth = true,
        //        MaxWidth = MaxWidth.Small,
        //    };

        //    var parameters = new DialogParameters<ProfileUpdateDialog>
        //{
        //    { p => p.UserProfileDetailsDto, userProfileDetailsDto },
        //    { p => p._Subjects, _subjectsDtos },
        //};

        //    var result = await DialogService.Show<ProfileUpdateDialog>("", parameters, options).Result;

        //    if (!result!.Canceled)
        //    {
        //        var updatedUserProfileDto = (EditUserProfileDto)result.Data!;

        //        updatedUserProfileDto.UserId = parsedId;

        //        var response = await BlazUserProfileService.EditUserProfileAsync(updatedUserProfileDto);

        //        if (response != null && response.StatusCode == HttpStatusCode.OK)
        //        {
        //            Snackbar.Add(Messages.Userprofileupdatedsuccessfully, Severity.Success);
        //            StateHasChanged();
        //        }
        //        else
        //        {
        //            Snackbar.Add(Messages.Failedtoupdateuserprofile, Severity.Error);
        //            StateHasChanged();
        //        }
        //    }
        //}

        //private async Task ViewProfileAsync(object id)
        //{
        //    var parsedId = Guid.Parse(id.ToString()!);

        //    var userProfileDetailsDto = await BlazUserProfileService.GetUserProfileDetailsAsync(parsedId);

        //    var options = new DialogOptions
        //    {
        //        CloseOnEscapeKey = true,
        //        FullWidth = true,
        //        MaxWidth = MaxWidth.Small,
        //    };

        //    var parameters = new DialogParameters<ProfileViewDialog>
        //{
        //    { p => p.UserProfileDetailsDto, userProfileDetailsDto },
        //};

        //    await DialogService.Show<ProfileViewDialog>("", parameters, options).Result;
        //}

        public async Task SyncUserFun()
        {
            SyncDisabled = true;

            var syncResult = await BlazUserService.SyncUsersProfiles(_globalUserContext.CurrentOrganizationSignature!);

            SyncDisabled = false;

            SyncData++;

            if (syncResult.CustomCodeStatus == Helper.Enums.CustomCodeStatus.Success ||
                syncResult.CustomCodeStatus == Helper.Enums.CustomCodeStatus.UserNotFound)
            {
                Snackbar.Add(syncResult.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(@Resource.AnErrorOccurred, Severity.Error);
            }

            StateHasChanged();
        }

        private async Task ViewUserAsync(object id)
        {
            var parsedId = Guid.Parse(id.ToString()!);

            var parameters = new DialogParameters<UserAccessDialog>
            {
                { x => x.UserId, parsedId }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                FullWidth = true,
                MaxWidth = MaxWidth.ExtraLarge
            };

            await DialogService.Show<UserAccessDialog>(Resource.UserAccessManagement, parameters, options).Result;
        }
    }
}
