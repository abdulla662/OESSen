using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.User;

namespace OES.Blazor.Components.GenericComponents.Login
{
    public partial class OrganizationTreeNodeComponent
    {
        [Parameter] public OrganizationTreeNodeDto Node { get; set; } = default!;
        [Parameter] public int Level { get; set; }
        [Parameter] public long? SelectedId { get; set; }
        [Parameter] public EventCallback<long> OnSelectOrganization { get; set; }
        [Parameter] public EventCallback<OrganizationTreeNodeDto> OnToggleNode { get; set; }

        private bool IsSelected => SelectedId == Node.Id;
        private bool HasChildren => Node.Children.Count > 0;
        private string LevelIcon => Level switch
        {
            0 => Icons.Material.Filled.Business,
            1 => Icons.Material.Filled.Domain,
            2 => Icons.Material.Filled.AccountBalance,
            _ => Icons.Material.Filled.Store
        };

        private async Task OnSelect() => await OnSelectOrganization.InvokeAsync(Node.Id);
        private async Task OnToggle() => await OnToggleNode.InvokeAsync(Node);
    }
}
