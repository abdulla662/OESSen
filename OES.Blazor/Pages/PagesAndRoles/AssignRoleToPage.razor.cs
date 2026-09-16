using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.PagesService;
using OES.Blazor.Services.Interfaces.RolesService;
using OES.Helper.Enums;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Helper.ResourceFiles;
using SharedHelper.RolesNames;
using System.Net;

namespace OES.Blazor.Pages.PagesAndRoles
{
    public partial class AssignRoleToPage : ComponentBase
    {
        [Inject] IBlazPagesService PageService { get; set; }
        [Inject] IBlazRolesService AppRoleService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] GlobalUserContext GlobalUserContext { get; set; }

        // Roles On Initiation
        private List<RoleDto> Roles { get; set; } = new();

        // Pages On Initiation
        private List<BalzPageDTO> Pages { get; set; } = new();

        private IEnumerable<RoleDto> CurrentRoles { get; set; } = new HashSet<RoleDto>();

        private BalzPageDTO SelectedPage { get; set; }

        private int GroupListComponentKey { get; set; }


        protected override async Task OnInitializedAsync()
        {
            if (!GlobalUserContext.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin))
            {
                NavigationManager.NavigateTo("/");
                return;
            }

            var res = await PageService.GetAll();
            Roles = await AppRoleService.GetAllRolesAsync();
            Pages = res?.Data as List<BalzPageDTO>;
        }

        private async Task<IEnumerable<BalzPageDTO>> SearchPagesAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Pages;
            }

            return Pages.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task<IEnumerable<RoleDto>> SearchRolesAsync(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Roles;
            }

            return Roles.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task OnSubmit()
        {
            if (SelectedPage == null || !CurrentRoles.Any())
            {
                return;
            }

            var assignRoleToPageDto = new AssignRoleToPageDTO
            {
                PageID = SelectedPage.ID,
                RoleIDs = CurrentRoles.Select(role => role.Id).ToList()
            };

            var apiResponse = await PageService.AssignRolesToPage(assignRoleToPageDto);

            if (apiResponse.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(Resource.RolesAssignedToPageSuccessfully, Severity.Success);

                SelectedPage = null;
                CurrentRoles = new HashSet<RoleDto>();

                GroupListComponentKey++;
            }
            else if (apiResponse.StatusCode == HttpStatusCode.Conflict)
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
            else
            {
                Snackbar.Add(Resource.FailedToAddRolesToTheSelectedPage, Severity.Error);
            }

            StateHasChanged();
        }

        private async Task GetPageRolesAsync(BalzPageDTO Dto)
        {
            SelectedPage = Dto;

            if (SelectedPage != null)
            {
                CurrentRoles = [];

                var fetchedRoles = await PageService.GetPageRoles(SelectedPage.ID); // make a service

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
