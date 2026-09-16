using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Components.GenericComponents.TreeViewTemplates.GrpcTreeViewTemplate;
using OES.Blazor.Pages.ILOTreeView.ILOInsertionDialogBox;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.ILOTreeView
{
    public partial class ILOTreeView : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazILOService BlazorILOService { get; set; }
        [Inject] private IILoTreeGrpcService IloTreeService { get; set; }
        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }
        [Inject] private IBlazGroupService BlazGroupService { get; set; }

        private string DeletedIloName { get; set; } = string.Empty;
        private bool IsComponentInitialized { get; set; }
        private TreeItemResponseDto CurrentItem { get; set; }
        private List<TreeItemResponseDto> Items { get; set; } = [];
        private List<TreeItemResponseDto> FlattenedTreeItems { get; set; } = [];
        private List<GetOESGroupDto> OesGroupsDtos { get; set; } = [];
        private static bool RightToLeft =>
            System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        private static TreeCrudButtonsRolesPlaceholders TreeCrudButtonsRolesPlaceholders =>
            new("",
                OesTemplateRoleConstants.IloCreator,
                OesTemplateRoleConstants.IloEditor,
                OesTemplateRoleConstants.IloDeleter,
                OesTemplateRoleConstants.IloViewer);

        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };


        protected override async Task OnInitializedAsync()
        {
            var storedParentId = await SessoinStorage.GetValue<long>("PerformViewBtnClick");

            var result = await IloTreeService.GetAllNestedIlosWithParentAsync(storedParentId);

            Items = result ?? [];
            FlattenedTreeItems = Items;

            foreach (var item in FlattenedTreeItems)
            {
                item.GroupsIds ??= [];
                item.Children ??= [];
            }

            IsComponentInitialized = true;

            OesGroupsDtos = await BlazGroupService.GetGroupsAsync() ?? [];
        }

        public async Task InsertItemInDbAsync(TreeItemResponseDto parentIloDto)
        {
            var cachedParent = FlattenedTreeItems.FirstOrDefault(x => x.Id == parentIloDto.Id);

            if (cachedParent == null)
                return;

            cachedParent.GroupsIds ??= [];
            cachedParent.Children ??= [];

            var parameters = new DialogParameters<ILOInsertionDialogBox.ILOInsertionDialogBox>
            {
                {
                    dialog => dialog.ILOInsertionDialogParams,
                    new IloInsertionOrUpdateDialogParameters
                    {
                        Name = "",
                        Description = "",
                        Code = "",
                        IsActive = true,
                        GroupsIds = [.. cachedParent.GroupsIds]
                    }
                },
                { dialog => dialog.OesGroupsDtos, OesGroupsDtos }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Medium
            };

            var result = await DialogService
                .Show<ILOInsertionDialogBox.ILOInsertionDialogBox>(
                    Resource.AddNewILO,
                    parameters,
                    options)
                .Result;

            if (result.Canceled || result.Data is null)
                return;

            var newIloData = (IloInsertionOrUpdateDialogParameters)result.Data;

            var newIlo = new ILOInsertionOrUpdateRequestDto(
                newIloData.Id,
                newIloData.Name,
                newIloData.Description,
                cachedParent.Id,
                newIloData.Code,
                newIloData.IsActive,
                cachedParent.OrganizationId,
                newIloData.GroupsIds ?? []
            );

            var response = await BlazorILOService.InsertNodeAsync(newIlo);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message ?? Resource.SomethingWentWrong, Severity.Error);
                return;
            }

            var insertedNode =
                JsonSerializer.Deserialize<TreeItemResponseDto>(
                    response.Data?.ToString() ?? string.Empty,
                    _jsonSerializerOptions);

            if (insertedNode != null)
            {
                insertedNode.GroupsIds = newIlo.GroupsIds;
                insertedNode.Children ??= new List<TreeItemResponseDto>();

                cachedParent.Children.Add(insertedNode);
                FlattenedTreeItems.Add(insertedNode);
            }

            StateHasChanged();

            Snackbar.Add(Resource.ILOhasjustbeeninsertedsuccessfully, Severity.Success);
        }

        public async Task UpdateItemInDbAsync(TreeItemResponseDto selectedIloDto)
        {
            selectedIloDto.GroupsIds ??= [];
            selectedIloDto.Children ??= [];
            selectedIloDto.Description ??= "";
            selectedIloDto.Code ??= "";

            var parents = await IloTreeService.GetAllowedParentsOfIloNodeAsync(selectedIloDto.ParentId);
            if (parents == null)
            {
                Snackbar.Add(Resource.Failedtofetchparentitems, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<ILOEditDialog.ILOEditDialog>
            {
                {
                    x => x.IloInsertionOrUpdateDialogParams,
                    new IloInsertionOrUpdateDialogParameters
                    {
                        Id = selectedIloDto.Id,
                        Name = selectedIloDto.Text,
                        Description = selectedIloDto.Description,
                        ParentId = selectedIloDto.ParentId,
                        Code = selectedIloDto.Code,
                        IsActive = selectedIloDto.IsActive,
                        GroupsIds = selectedIloDto.GroupsIds
                    }
                },
                { x => x.OesGroupsDtos, OesGroupsDtos },
                { x => x.Parents, parents }
            };

            var dialog = await DialogService.ShowAsync<ILOEditDialog.ILOEditDialog>(
                Resource.EditILO,
                parameters,
                new DialogOptions { CloseOnEscapeKey = true, FullWidth = true, MaxWidth = MaxWidth.Medium }
            );

            var result = await dialog.Result;
            if (result.Canceled)
                return;

            var updatedIloData = (IloInsertionOrUpdateDialogParameters)result.Data;
            updatedIloData.GroupsIds ??= [];

            var updatedIlo = new ILOInsertionOrUpdateRequestDto(
                updatedIloData.Id,
                updatedIloData.Name,
                updatedIloData.Description,
                updatedIloData.ParentId,
                updatedIloData.Code,
                updatedIloData.IsActive,
                updatedIloData.OrganizationId,
                updatedIloData.GroupsIds
            );

            var response = await BlazorILOService.EditNodeAsync(updatedIlo);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message ?? Resource.Somethingwentwrongupdating, Severity.Error);
                return;
            }

            Snackbar.Add(Resource.ILOhasjustbeenupdatedsuccessfully, Severity.Success);
            await OnInitializedAsync();
            await InvokeAsync(StateHasChanged);
        }

        public async Task RemoveItemFromDbAsync(TreeItemResponseDto treeItem)
        {
            DeletedIloName = treeItem.Text;
            CurrentItem = treeItem;

            var parameters = new DialogParameters
            {
                { nameof(GenericDialog.Title), Resource.AreYouSure },
                { nameof(GenericDialog.Content), $"{Resource.Doyouwanttodelete} {DeletedIloName} {Resource.QuestionMark}"},
                { nameof(GenericDialog.SubmitText), Resource.Delete },
                { nameof(GenericDialog.SubmitButtonColor), Color.Error },
                { nameof(GenericDialog.SubmitButtonStartIcon), Icons.Material.Filled.Delete },
                { nameof(GenericDialog.ShowAdditionalButton), treeItem.Children.Count > 0 },
                { nameof(GenericDialog.AdditionalButtonText), Resource.DeleteThenTransferChilds },
                { nameof(GenericDialog.AdditionalButtonStartIcon), Icons.Material.Filled.DeleteSweep },
                { nameof(GenericDialog.AdditionalButtonColor), Color.Primary },
                { nameof(GenericDialog.CancelText), Resource.Cancel },
                { nameof(GenericDialog.CancelButtonStartIcon), Icons.Material.Filled.Close },
                { nameof(GenericDialog.AdditionalButtonCallBack), EventCallback.Factory.Create(this, () => TransferChildrenForNodeAsync(treeItem)) },
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Small,
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                Resource.ConfirmDelete,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is true)
            {
                await DeleteItemFromDbAsync(CurrentItem);
            }
        }

        public async Task DeleteItemFromDbAsync(TreeItemResponseDto treeItem)
        {
            var response = await BlazorILOService.ExecuteSoftDeleteForNodeAsync(treeItem.Id);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var parentItem = FlattenedTreeItems.Find(x => x.Id == treeItem.ParentId);
                parentItem?.Children.Remove(treeItem);
                FlattenedTreeItems.Remove(treeItem);
                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            StateHasChanged();
        }

        public async Task TransferChildrenForNodeAsync(TreeItemResponseDto treeItem)
        {
            var response = await BlazorILOService.TransferChildrenForNodeAsync(treeItem.Id);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var parentItem = FlattenedTreeItems.Find(x => x.Id == treeItem.ParentId);
                var children = treeItem.Children.ToList();

                parentItem?.Children.Remove(treeItem);
                FlattenedTreeItems.Remove(treeItem);

                foreach (var child in children)
                {
                    child.ParentId = parentItem.Id;
                    parentItem.Children.Add(child);
                }

                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            StateHasChanged();
        }

        public async Task ShowItemDetailsAsync(TreeItemResponseDto treeItem)
        {
            var parameters = new DialogParameters<ILODetailsDialogBox.ILODetailsDialogBox>
            {
                { d => d.ILODetailsObject, treeItem },
                { d => d.OesGroupsDtos, OesGroupsDtos }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                BackdropClick = false,
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };

            await DialogService.ShowAsync<ILODetailsDialogBox.ILODetailsDialogBox>(
                Resource.ILODetails,
                parameters,
                options
            );
        }
    }
}
