using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.RolesService;
using OES.Blazor.Services.Interfaces.UserService;
using OES.Helper.Dtos.User;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.Enums;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.UsersAndRoles
{
    public partial class AssignRoleToUser : ComponentBase
    {
        [Inject] IBlazUserService UserService { get; set; }
        [Inject] IBlazRolesService AppRoleService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        // Roles On Initiation
        private List<RoleDto> Roles { get; set; } = new();

        // Pages On Initiation
        private List<BlazUserDTO> Users { get; set; } = new();

        private IEnumerable<RoleDto> CurrentRoles { get; set; } = new HashSet<RoleDto>();

        private BlazUserDTO SelectedUser { get; set; }

        private int GroupListComponentKey { get; set; }


        protected override async Task OnInitializedAsync()
        {
            var res = await UserService.GetAll();
            Roles = await AppRoleService.GetAllRolesAsync();
            Users = res.Data as List<BlazUserDTO>;
        }


        private async Task<IEnumerable<BlazUserDTO>> SearchPathsAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Users;
            }

            return Users.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task OnSubmit()
        {
            if (SelectedUser == null || !CurrentRoles.Any())
            {
                return;
            }

            var assignRoleToPathDto = new AssignRoleToUserDto
            {
                UserID = SelectedUser.ID,
                RoleIDs = CurrentRoles.Select(role => role.Id).ToList()
            };

            var apiResponse = await UserService.AssignRolesToUser(assignRoleToPathDto);

            if (apiResponse.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(Resource.RolesAssignedToUserSuccessfully, Severity.Success);

                SelectedUser = null;

                CurrentRoles = new HashSet<RoleDto>();

                GroupListComponentKey++;
            }
            else if (apiResponse.StatusCode == HttpStatusCode.Conflict)
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
            else
            {
                Snackbar.Add(Resource.FailedToAddRolesToTheSelectedUser, Severity.Error);
            }

            StateHasChanged();
        }

        private async Task GetPathRolesAsync(BlazUserDTO Dto)
        {
            SelectedUser = Dto;

            if (SelectedUser != null)
            {
                CurrentRoles = [];

                var fetchedRoles = await UserService.GetUserRoles(SelectedUser.ID);

                var Selected = new List<RoleDto>();

                foreach (var org in fetchedRoles)
                {
                    if (Roles.Exists(x => x.Id == org.Id))
                        Selected.Add(Roles.Find(x => x.Id == org.Id));
                }

                CurrentRoles = Selected;
            }
            else
            {
                CurrentRoles = [];
            }
        }
    }
}
