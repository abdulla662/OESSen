using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.OrganizationStructure;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.OrganizationStructureService;
using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.OrganizationStructure
{
    public partial class OrganizationStructureTreePage
    {
        [Inject] private IDialogService DialogService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private IBlazOrganizationStructureService BlazOrganizationStructureService { get; set; }

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        private List<TreeItemResponseDto> Items { get; set; }

        private List<TreeItemResponseDto> FlattenedTreeItems { get; set; }

        private bool IsComponentInitialized { get; set; }


        protected override async Task OnInitializedAsync()
        {
            long rootId = await BlazSessionStorageService.GetValue<long>("PerformViewBtnClick");

            Items = FlattenedTreeItems = await BlazOrganizationStructureService.GetOrganizationStructureTreeByRootId(rootId);

            IsComponentInitialized = true;
        }

        public async Task InsertItemInDbAsync(TreeItemResponseDto parentTreeItem)
        {
            if (parentTreeItem.Children.Count > 0)
            {
                Snackbar.Add(Resource.CannotAddSiblingToSameParentDueToTreeStructure, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<AddOrUpdateOrganizationStructureNodeDialog>
            {
                {
                    x => x.Model,
                    new AddOrUpdateOrganizationStructureNodeRequestDto(parentTreeItem?.Id)
                }
            };

            DialogOptions options = new()
            {
                CloseOnEscapeKey = true,
                FullWidth = true,
                BackdropClick = false,
                MaxWidth = MaxWidth.Medium
            };

            var result = await DialogService.Show<AddOrUpdateOrganizationStructureNodeDialog>(Resource.AddOrganizationNode, parameters, options).Result;

            if (!result.Canceled)
            {
                var newOrganizationNodeDto = result.Data as AddOrUpdateOrganizationStructureNodeResponseDto;

                var newInsertedTreeItemDto = new TreeItemResponseDto
                {
                    Id = newOrganizationNodeDto.Id,
                    Text = newOrganizationNodeDto.Name,
                    Description = newOrganizationNodeDto.Description,
                    ParentId = newOrganizationNodeDto.ParentId,
                    Children = [],
                    IsExpanded = false,
                    Signature = newOrganizationNodeDto.OrganizationSignature,
                    IsLeaf = newOrganizationNodeDto.IsLeaf,
                    IsActive = newOrganizationNodeDto.IsActive,
                };

                parentTreeItem?.Children.Add(newInsertedTreeItemDto);

                FlattenedTreeItems.Add(newInsertedTreeItemDto);

                StateHasChanged();
            }
        }

        public async Task UpdateItemInDbAsync(TreeItemResponseDto selectedTreeItem)
        {
            var parameters = new DialogParameters<AddOrUpdateOrganizationStructureNodeDialog>
            {
                {
                    x => x.Model,
                    new AddOrUpdateOrganizationStructureNodeRequestDto(
                        selectedTreeItem.Id,
                        selectedTreeItem.Text,
                        selectedTreeItem.Description,
                        selectedTreeItem.ParentId,
                        selectedTreeItem.IsLeaf
                    )
                }
            };

            DialogOptions options = new()
            {
                CloseOnEscapeKey = true,
                FullWidth = true,
                BackdropClick = false,
                MaxWidth = MaxWidth.Medium
            };

            var result = await DialogService.Show<AddOrUpdateOrganizationStructureNodeDialog>(Resource.UpdateOrganizationNode, parameters, options).Result;

            if (!result.Canceled)
            {
                var updatedOrganizationNodeDto = result.Data as AddOrUpdateOrganizationStructureNodeResponseDto;

                selectedTreeItem.Text = updatedOrganizationNodeDto.Name;
                selectedTreeItem.Description = updatedOrganizationNodeDto.Description;
                selectedTreeItem.IsLeaf = updatedOrganizationNodeDto.IsLeaf;

                StateHasChanged();
            }
        }

        public async Task RemoveItemFromDbAsync(TreeItemResponseDto selectedTreeItem)
        {
            var parameters = new DialogParameters<GenericDialog>()
            {
                { x => x.Title, Resource.Alert },
                { x => x.Content, string.Format(Resource.DeleteNodeConfirmation, selectedTreeItem.Text) },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                BackdropClick = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var apiResponse = await BlazOrganizationStructureService.DeleteOrganizationStructureNodeAsync(selectedTreeItem.Id);

                if (apiResponse.StatusCode == HttpStatusCode.OK)
                {
                    var parentTreeItem = FlattenedTreeItems.Find(item => item.Id == selectedTreeItem.ParentId);

                    parentTreeItem.Children.Remove(selectedTreeItem);

                    FlattenedTreeItems.Remove(selectedTreeItem);

                    Snackbar.Add(apiResponse.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(apiResponse.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        public async Task AddOrUpdateNodeLookupItemsAsync(TreeItemResponseDto targetTreeItem)
        {
            var parameters = new DialogParameters<AddOrUpdateOrganizationNodeLookupItemDialog>
            {
                {
                    x => x.Model,
                    new AddOrUpdateOrganizationNodeLookupItemRequestDto(targetTreeItem.Id)
                },
                {
                    x => x.TargetOrganizationNodeParentId,
                    targetTreeItem.ParentId
                },
                {
                    x => x.IsParentOfThisNodeRepresentsTheTreeRootNode,
                    FlattenedTreeItems.Find(item => item.Id == targetTreeItem.ParentId).ParentId == null
                },
                {
                    x => x.TargetOrganizationNodeParentName,
                    FlattenedTreeItems.Find(item => item.Id == targetTreeItem.ParentId)?.Text ?? string.Empty
                },
                {
                    x => x.TargetOrganizationNodeName,
                    targetTreeItem.Text
                },
                {
                    x => x.IsTargetOrganizationNodeLeaf,
                    targetTreeItem.IsLeaf
                },
            };

            DialogOptions options = new()
            {
                CloseOnEscapeKey = true,
                FullWidth = true,
                MaxWidth = MaxWidth.Large,
                BackdropClick = false
            };

            await DialogService.Show<AddOrUpdateOrganizationNodeLookupItemDialog>(string.Empty, parameters, options).Result;

            StateHasChanged();
        }
    }
}
