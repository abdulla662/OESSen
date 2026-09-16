using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Pages.ILOTreeView.ILODetailsDialogBox
{
    public partial class ILODetailsDialogBox : ComponentBase
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public TreeItemResponseDto ILODetailsObject { get; set; } = new();

        [Parameter] public List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];

        public List<GetOESGroupDto> GetCurrentItemBankNodeGroups()
        {
            return OesGroupsDtos.IntersectBy(ILODetailsObject.GroupsIds, static x => x.Id).ToList();
        }

        void Close() => MudDialog.Cancel();
    }
}
