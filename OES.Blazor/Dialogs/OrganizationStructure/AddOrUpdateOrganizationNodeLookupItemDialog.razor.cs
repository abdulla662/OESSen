using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Dialogs.OrganizationStructure
{
    public partial class AddOrUpdateOrganizationNodeLookupItemDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private IDialogService DialogService { get; set; } = default!;

        [Inject] private IBlazOrganizationStructureService BlazOrganizationStructureService { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public AddOrUpdateOrganizationNodeLookupItemRequestDto Model { get; set; } = new();

        [Parameter] public long? TargetOrganizationNodeParentId { get; set; }

        [Parameter] public string TargetOrganizationNodeParentName { get; set; }

        [Parameter] public string TargetOrganizationNodeName { get; set; }

        [Parameter] public bool IsTargetOrganizationNodeLeaf { get; set; }

        [Parameter] public bool IsParentOfThisNodeRepresentsTheTreeRootNode { get; set; }

        private List<GetOrganizationNodeLookupItemResponseDto> ParentOrganizationNodeLookupItemsDtos { get; set; } = [];

        private List<GetOrganizationNodeLookupItemResponseDto> ChildOrganizationNodeLookupItemsDtos { get; set; } = [];

        private bool IsUpdateMode => Model.Id > 0;


        private MudTextField<string> firstInputRef;

        private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        private string searchFieldString = string.Empty;


        protected override async Task OnInitializedAsync()
        {
            var parentNodeLookupItemsTask = BlazOrganizationStructureService.GetNodeLookupItemsAsync(TargetOrganizationNodeParentId ?? 0);
            var childNodeLookupItemsTask = BlazOrganizationStructureService.GetNodeLookupItemsAsync(Model.OrganizationStructureNodeId);

            await Task.WhenAll(parentNodeLookupItemsTask, childNodeLookupItemsTask);

            ParentOrganizationNodeLookupItemsDtos = await parentNodeLookupItemsTask;
            ChildOrganizationNodeLookupItemsDtos = await childNodeLookupItemsTask;

            StateHasChanged();
        }

        private async Task SubmitAsync()
        {
            if (string.IsNullOrWhiteSpace(Model.Name))
            {
                Snackbar.Add(Resource.NodeLookupItemNameRequired, Severity.Error);
                return;
            }

            if (Model.ParentLookupItemId == null && !IsParentOfThisNodeRepresentsTheTreeRootNode && !IsTargetOrganizationNodeLeaf)
            {
                Snackbar.Add(Resource.ParentLookupItemRequired, Severity.Error);
                return;
            }

            if (IsUpdateMode)
            {
                await UpdateNodeLookupItemAsync();
            }
            else
            {
                await AddNodeLookupItemAsync();
            }
        }

        private async Task AddNodeLookupItemAsync()
        {
            var apiResponse = await BlazOrganizationStructureService.AddNodeLookupItemAsync(Model);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(apiResponse.Message, Severity.Success);

                var returnedLookupItemDto = JsonSerializer.Deserialize<AddOrUpdateOrganizationNodeLookupItemResponseDto>(apiResponse.Data.ToString(), jsonSerializerOptions);

                ChildOrganizationNodeLookupItemsDtos = ChildOrganizationNodeLookupItemsDtos.Prepend(new GetOrganizationNodeLookupItemResponseDto(
                    returnedLookupItemDto.Id,
                    returnedLookupItemDto.Name,
                    returnedLookupItemDto.ParentLookupItemId,
                    returnedLookupItemDto.OrganizationStructureNodeId
                ))
                .ToList();

                ResetFormModel(returnedLookupItemDto.OrganizationStructureNodeId);
            }
            else
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
        }

        private async Task UpdateNodeLookupItemAsync()
        {
            var apiResponse = await BlazOrganizationStructureService.UpdateNodeLookupItemAsync(Model);

            if (apiResponse.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(apiResponse.Message, Severity.Success);

                var returnedLookupItemDto = JsonSerializer.Deserialize<AddOrUpdateOrganizationNodeLookupItemResponseDto>(apiResponse.Data.ToString(), jsonSerializerOptions);

                var targetOrganizationNodeLookupItem = ChildOrganizationNodeLookupItemsDtos.Find(x => x.Id == returnedLookupItemDto.Id);

                if (targetOrganizationNodeLookupItem == null) return;

                targetOrganizationNodeLookupItem.Name = returnedLookupItemDto.Name;
                targetOrganizationNodeLookupItem.ParentLookupItemId = returnedLookupItemDto.ParentLookupItemId;

                ResetFormModel(returnedLookupItemDto.OrganizationStructureNodeId);
            }
            else
            {
                Snackbar.Add(apiResponse.Message, Severity.Error);
            }
        }

        private async Task DeleteNodeLookupItemAsync(GetOrganizationNodeLookupItemResponseDto targetLookupItemDto)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, string.Format(Resource.AreYouSureYouWantToDeleteThisNode, targetLookupItemDto.Name) },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var apiResponse = await BlazOrganizationStructureService.DeleteNodeLookupItemAsync(targetLookupItemDto.Id);

                if (apiResponse.StatusCode == HttpStatusCode.OK)
                {
                    ChildOrganizationNodeLookupItemsDtos.Remove(ChildOrganizationNodeLookupItemsDtos.Find(x => x.Id == targetLookupItemDto.Id));

                    Snackbar.Add(apiResponse.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(apiResponse.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private async Task ViewLookupItemChildsAsync(GetOrganizationNodeLookupItemResponseDto targetLookupItemDto)
        {
            var parameters = new DialogParameters<ViewOrganizationLookupItemsDialog>()
            {
                {x => x.ParentLookupItemDto , targetLookupItemDto}
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium
            };

            await DialogService.Show<ViewOrganizationLookupItemsDialog>(Resource.ViewChildLookupItems, parameters, options).Result;

            StateHasChanged();
        }

        private async Task PrepareFormForLookupItemUpdateAsync(GetOrganizationNodeLookupItemResponseDto lookupItem)
        {
            Model.Id = lookupItem.Id;
            Model.Name = lookupItem.Name;
            Model.ParentLookupItemId = lookupItem.ParentLookupItemId;
            Model.OrganizationStructureNodeId = lookupItem.OrganizationStructureNodeId;
            if (firstInputRef != null)
            {
                await firstInputRef.FocusAsync();
            }
        }

        private bool FilterFuncWrapper(GetOrganizationNodeLookupItemResponseDto lookupItem) => FilterFunc(lookupItem, searchFieldString);

        private bool FilterFunc(GetOrganizationNodeLookupItemResponseDto lookupItem, string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
                return true;
            if (lookupItem.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                return true;
            if (ParentOrganizationNodeLookupItemsDtos.Find(x => x.Id == lookupItem.ParentLookupItemId)?.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false)
                return true;
            return false;
        }

        private void ResetFormModel(long currentOrganizationStructureNodeId) => Model = new AddOrUpdateOrganizationNodeLookupItemRequestDto(currentOrganizationStructureNodeId);

        private void Close() => MudDialog.Cancel();
    }
}
