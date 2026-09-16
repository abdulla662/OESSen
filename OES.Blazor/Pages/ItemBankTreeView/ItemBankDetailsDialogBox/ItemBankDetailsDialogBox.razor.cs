using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Pages.ItemBankTreeView.ItemBankDetailsDialogBox
{
    public partial class ItemBankDetailsDialogBox
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public TreeItemResponseDto ItemBankDetailsObject { get; set; }

        [Parameter] public List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];

        public List<GetOESGroupDto> GetCurrentItemBankNodeGroups()
        {
            return [.. OesGroupsDtos.IntersectBy(ItemBankDetailsObject.GroupsIds, static x => x.Id)];
        }

        void Close() => MudDialog.Cancel();
    }
}
