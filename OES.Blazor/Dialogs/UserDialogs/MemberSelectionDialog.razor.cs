using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Helper.Dtos.User;
using OES.Helper.General;

namespace OES.Blazor.Dialogs.UserDialogs
{
    public partial class MemberSelectionDialog : ComponentBase
    {
        [Inject] private IBlazUserProfileService BlazUserProfileService { get; set; } = default!;

        [Parameter] public List<BlazUserDTO> InitialSelectedMembers { get; set; } = [];
        [Parameter] public List<BlazUserDTO> LockedMembers { get; set; } = [];

        private Task<CustomTableData<BlazUserDTO>> GetUsersForSelectionAsync(PaginationSearchModel paginationSearchModel)
        {
            return BlazUserProfileService.GetUsersForSelectionAsync(paginationSearchModel);
        }

        private static List<BlazUserDTO> ConvertToResult(List<BlazUserDTO> selectedUsers)
        {
            return selectedUsers;
        }
    }
}
