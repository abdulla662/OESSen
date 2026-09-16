using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Helper.Dtos.User;
using OES.Helper.General;

namespace OES.Blazor.Dialogs.UserDialogs
{
    public partial class UserSelectionDialog : ComponentBase
    {
        [Inject] private IBlazUserProfileService BlazUserProfileService { get; set; } = default!;

        [Parameter] public List<BlazUserDTO> InitialSelectedUsers { get; set; } = [];

        private Task<CustomTableData<BlazUserDTO>> GetUsersForSelectionAsync(PaginationSearchModel paginationSearchModel)
        {
            return BlazUserProfileService.GetUsersForSelectionAsync(paginationSearchModel);
        }

        private static List<BlazUserDTO> ConvertUsersToResult(List<BlazUserDTO> selectedUsers)
        {
            return selectedUsers;
        }
    }
}
