using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Components.GenericComponents.OrganizationStructure
{
    public partial class GenericOrganizationStructureControlPanel
    {
        [Inject] private IBlazOrganizationStructureService BlazOrganizationStructureService { get; set; }

        [Parameter] public ComponentUsageMode UsageMode { get; set; }
        [Parameter] public string[] CssClasses { get; set; } = [];
        [Parameter] public bool DisplayHorizontal { get; set; }
        [Parameter] public List<long> PreSelectedLookupItemIds { get; set; } = [];
        [Parameter] public bool IsReadOnly { get; set; } = false;

        private GetOrganizationNodeResponseDto[] FlattenedOrganizationStructureNodes { get; set; } = [];
        private GetOrganizationRootResponseDto[] GetAllOrganizationStructureRoots { get; set; } = [];
        private GetOrganizationRootResponseDto SelectedRoot { get; set; }
        private string WarningMessage { get; set; } = null;
        private Dictionary<(long Id, string Name, string Description, bool IsFirstLevelChildOfRootNode), GetOrganizationNodeLookupItemResponseDto[]> NodeLookupItemsDictionary { get; set; } = [];
        private Dictionary<long, GetOrganizationNodeLookupItemResponseDto> SelectedNodeLookupItemDictionary { get; set; } = [];
        private bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            StateHasChanged();

            GetAllOrganizationStructureRoots = await BlazOrganizationStructureService.GetAllOrganizationStructureRootsAsync();

            if (PreSelectedLookupItemIds?.Count > 0)
            {
                var response = await BlazOrganizationStructureService.GetOrganizationRootIdByLookupIdsAsync(PreSelectedLookupItemIds);

                if (response.StatusCode == HttpStatusCode.OK && response.Data != null && response.Data is JsonElement jsonElement)
                {
                    var rootDto = jsonElement.Deserialize<GetOrganizationRootResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    SelectedRoot = GetAllOrganizationStructureRoots?.FirstOrDefault(r => r.Id == rootDto.Id);

                    if (SelectedRoot != null)
                    {
                        await OnRootChanged(SelectedRoot);
                    }
                }
            }
            else if (SelectedRoot != null)
            {
                await LoadOrganizationStructureByRootAsync();
            }

            IsLoading = false;
            StateHasChanged();
        }

        private async Task OnRootChanged(GetOrganizationRootResponseDto newRoot)
        {
            SelectedRoot = newRoot;
            await LoadOrganizationStructureByRootAsync();
        }

        private async Task LoadOrganizationStructureByRootAsync()
        {
            if (SelectedRoot == null)
                return;

            IsLoading = true;
            WarningMessage = null;
            StateHasChanged();

            var response = await BlazOrganizationStructureService.GetFlattenedOrganizationStructureAsync(SelectedRoot.Id);

            if (response.Length == 0)
            {
                WarningMessage = "No organization structure nodes found under this root";
                FlattenedOrganizationStructureNodes = [];
                NodeLookupItemsDictionary = [];
                SelectedNodeLookupItemDictionary = [];
                IsLoading = false;
                StateHasChanged();
                return;
            }

            FlattenedOrganizationStructureNodes = response;

            NodeLookupItemsDictionary = FlattenedOrganizationStructureNodes.ToDictionary(
                x => (x.Id, x.Name, x.Description, x.IsFirstLevelChildOfRootNode),
                x => x.IsFirstLevelChildOfRootNode
                    ? [.. x.LookupItems]
                    : x.LookupItems.TrueForAll(i => i.ParentLookupItemId == null)
                        ? x.LookupItems.ToArray()
                        : []
            );

            SelectedNodeLookupItemDictionary = FlattenedOrganizationStructureNodes.ToDictionary(x => x.Id, _ => null as GetOrganizationNodeLookupItemResponseDto);

            ApplyPreSelectedLookupItems();

            IsLoading = false;
            StateHasChanged();
        }

        private void ApplyPreSelectedLookupItems()
        {
            foreach (var id in PreSelectedLookupItemIds)
            {
                var lookupItem = FlattenedOrganizationStructureNodes
                    .SelectMany(x => x.LookupItems)
                    .FirstOrDefault(i => i.Id == id);

                if (lookupItem != null)
                {
                    SelectedNodeLookupItemDictionary[lookupItem.OrganizationStructureNodeId] = lookupItem;

                    var childNode = Array.Find(FlattenedOrganizationStructureNodes, x => x.LookupItems.Exists(i => i.ParentLookupItemId == lookupItem.Id));

                    if (childNode != null)
                    {
                        var filteredChildren = childNode
                            .LookupItems
                            .Where(x => x.ParentLookupItemId == lookupItem.Id)
                            .ToArray();

                        NodeLookupItemsDictionary[(childNode.Id, childNode.Name, childNode.Description, childNode.IsFirstLevelChildOfRootNode)] = filteredChildren;
                    }
                }
            }
        }

        private void OnLookupItemChanged(GetOrganizationNodeLookupItemResponseDto selectedLookupItem)
        {
            if (IsReadOnly) return;

            SelectedNodeLookupItemDictionary[selectedLookupItem.OrganizationStructureNodeId] = selectedLookupItem;

            foreach (var key in NodeLookupItemsDictionary.Keys)
            {
                if (key.Id > selectedLookupItem.OrganizationStructureNodeId && (key.IsFirstLevelChildOfRootNode || NodeLookupItemsDictionary[key].ToList().TrueForAll(x => x.ParentLookupItemId == null))) // To catch root lookup items and Is-Leaf-Nodes lookup items.
                {
                    break;
                }
                else if (key.Id > selectedLookupItem.OrganizationStructureNodeId)
                {
                    NodeLookupItemsDictionary[key] = [];
                    SelectedNodeLookupItemDictionary[key.Id] = null;
                }
            }

            var targetCascadedNode = Array.Find(FlattenedOrganizationStructureNodes, x => x.LookupItems.Exists(x => x.ParentLookupItemId == selectedLookupItem.Id));

            var filteredLookupItems = targetCascadedNode.LookupItems.Where(x => x.ParentLookupItemId == selectedLookupItem.Id).ToArray();

            NodeLookupItemsDictionary[(targetCascadedNode.Id, targetCascadedNode.Name, targetCascadedNode.Description, targetCascadedNode.IsFirstLevelChildOfRootNode)] = filteredLookupItems;
        }

        public GetOrganizationNodeLookupItemResponseDto[] GetSelectedLookupItems()
        {
            if (IsReadOnly) return [];

            if (UsageMode == ComponentUsageMode.Command)
            {
                return [.. SelectedNodeLookupItemDictionary.Values.Where(x => x?.Id > 0)];
            }
            else if (UsageMode == ComponentUsageMode.Query)
            {
                return [.. SelectedNodeLookupItemDictionary.Values.Where(x => x?.Id > 0)];
            }

            return [];
        }
    }

    public enum ComponentUsageMode
    {
        Command,
        Query
    }
}