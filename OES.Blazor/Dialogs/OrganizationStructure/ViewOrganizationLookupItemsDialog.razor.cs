using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos.OrganizationStructure.Responses;

namespace OES.Blazor.Dialogs.OrganizationStructure
{
    public partial class ViewOrganizationLookupItemsDialog
    {
        [Inject] private IBlazOrganizationStructureService BlazOrganizationStructureService { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public GetOrganizationNodeLookupItemResponseDto ParentLookupItemDto { get; set; }

        private List<GetOrganizationNodeLookupItemForViewResponseDto> LookupItemsDtos { get; set; } = [];

        private string searchFieldString = string.Empty;

        private bool isLoading = true;

        protected override async Task OnInitializedAsync()
        {
            LookupItemsDtos = await BlazOrganizationStructureService.GetChildLookupItemsByParentLookupItemIdAsync(ParentLookupItemDto.Id);

            isLoading = false;

            StateHasChanged();
        }

        private bool FilterFuncWrapper(GetOrganizationNodeLookupItemForViewResponseDto lookupItem) => FilterFunc(lookupItem, searchFieldString);

        private static bool FilterFunc(GetOrganizationNodeLookupItemForViewResponseDto lookupItem, string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return true;
            if (lookupItem.LookupItemName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            if (lookupItem.OrganizationStructureNodeName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private void Close() => MudDialog.Cancel();
    }
}
