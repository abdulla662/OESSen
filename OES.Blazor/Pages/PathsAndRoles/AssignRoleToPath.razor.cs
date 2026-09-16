using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.MemoryCache;
using OES.Blazor.Services.Interfaces.PathsService;
using OES.Blazor.Services.Interfaces.RolesService;
using OES.Helper.Enums;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.PathsAndRoles
{
    public partial class AssignRoleToPath : ComponentBase
    {
        [Inject] IBlazPathsService _PathService { get; set; }
        [Inject] IBlazRolesService AppRoleService { get; set; }
        [Inject] IBlazMemoryCache _BlazMemoryCache { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }

        // Roles On Initiation
        private List<RoleDto> Roles { get; set; } = new();

        // Pages On Initiation
        private List<BlazPathDTO> Paths { get; set; } = new();

        private IEnumerable<RoleDto> CurrentRoles { get; set; } = new HashSet<RoleDto>();

        private BlazPathDTO SelectedPath { get; set; }

        private int GroupListComponentKey { get; set; }


        protected override async Task OnInitializedAsync()
        {
            var res = await _PathService.GetAll();
            Roles = await AppRoleService.GetAllRolesAsync();
            Paths = res.Data as List<BlazPathDTO>;
        }


        private async Task<IEnumerable<BlazPathDTO>> SearchPathsAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Paths;
            }

            return Paths.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }


        private async Task<IEnumerable<RoleDto>> SearchRolesAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Roles;
            }

            return Roles.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }


        private async Task OnSubmit()
        {
            if (SelectedPath == null || !CurrentRoles.Any())
            {
                return;
            }

            var assignRoleToPathDto = new AssignRoleToEndpointDto
            {
                PathID = SelectedPath.ID,
                RoleIDs = CurrentRoles.Select(role => role.Id).ToList()
            };

            var apiResponse = await _PathService.AssignRolesToPath(assignRoleToPathDto);

            if (apiResponse.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(Resource.RolesAssignedToPathSuccessfully, Severity.Success);

                SelectedPath = null;
                CurrentRoles = new HashSet<RoleDto>();

                GroupListComponentKey++;
            }
            else if (apiResponse.StatusCode == HttpStatusCode.Conflict)
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
            else
            {
                Snackbar.Add(Resource.FailedToAddRolesToTheSelectedPath, Severity.Error);
            }

            StateHasChanged();
        }

        private async Task GetPathRolesAsync(BlazPathDTO Dto)
        {
            SelectedPath = Dto;

            if (SelectedPath != null)
            {
                CurrentRoles = [];
                var fetchedRoles = await _PathService.GetPathRoles(SelectedPath.ID);
                var Selected = new List<RoleDto>();
                foreach (var org in fetchedRoles)
                {
                    if (Roles.Any(x => x.Id == org.Id))
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
