using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.CreateGroupTemplate;
using OES.Helper.Dtos.CreateTemplate;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Components.GenericComponents.GroupRolesSelectionDialogs
{
    public partial class GenericGroupSelectorDialog<TEntity>
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public EventCallback<Guid> OnConfirmDelete { get; set; }
        [Parameter] public List<TEntity> PreSelectedItems { get; set; } = [];
        [Parameter] public Func<PaginationSearchModel, Task<CustomTableData<TEntity>>> EndpointService { get; set; }
        [Parameter] public ResourceType ResourceType { get; set; }
        [Parameter] public string Caption { get; set; } = Resource.AvailableItems;
        [Parameter] public EventCallback<List<TEntity>> SelectedItemsChanged { get; set; }
        [Parameter] public bool IsOpenedFromAI { get; set; } = false;

        private List<TEntity> _selectedItems = [];


        protected override void OnInitialized()
        {
            if (PreSelectedItems?.Any() == true)
                _selectedItems = [.. PreSelectedItems];
        }

        private static string GetItemName(TEntity item)
        {
            var nameProperty = item?.GetType().GetProperty("Name")?.GetValue(item)?.ToString();
            return string.IsNullOrWhiteSpace(nameProperty) ? Resource.Unnamed : nameProperty;
        }

        private void RemoveItem(TEntity item)
        {
            _selectedItems.Remove(item);
            Snackbar.Add(string.Format(Resource.ItemRemoved, GetItemName(item)), Severity.Info);
        }

        private async Task OpenGenericDialog()
        {
            Func<PaginationSearchModel, Task<CustomTableData<TEntity>>> sortedEndpoint = async pagination =>
            {
                var data = await EndpointService(pagination);
                data.Items = [.. data.Items.OrderByDescending(item => item?.GetType().GetProperty("Id")?.GetValue(item))];
                return data;
            };

            var parameters = new DialogParameters
            {
                { DialogParameterKeys.Caption, Caption },
                { DialogParameterKeys.DisplayColumns, new[] { DialogParameterKeys.Name } },
                { DialogParameterKeys.EndpointService, sortedEndpoint },
                { DialogParameterKeys.ResourceType, ResourceType },
                { DialogParameterKeys.OnConfirmDelete, OnConfirmDelete },
                { nameof(PreSelectedItems), PreSelectedItems }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true
            };

            var dialog = await DialogService.ShowAsync<GenericGroupSelectionDialog<TEntity>>($"{Resource.Select} {Caption}", parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is List<TEntity> selected)
            {
                _selectedItems = selected;
                PreSelectedItems = selected;
                await SelectedItemsChanged.InvokeAsync(selected);
                StateHasChanged();
                Snackbar.Add(string.Format(Resource.SelectionSuccessful, Caption), Severity.Success);
            }
        }

        private async Task OpenCreateTemplateDialog()
        {
            var parameters = new DialogParameters
            {
                { DialogParameterKeys.TypeOfResource, ResourceType }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            };

            var dialog = await DialogService.ShowAsync<CreateTemplateDialog>(Resource.CreateTemplate, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is CreateTemplateDto createdTemplate)
            {
                var freshData = await EndpointService(new PaginationSearchModel());
                freshData.Items = [.. freshData.Items.OrderByDescending(item => item?.GetType().GetProperty("Id")?.GetValue(item))];

                var newlyCreated = freshData.Items
                    .FirstOrDefault(item =>
                        item?.GetType().GetProperty(nameof(createdTemplate.Name))?.GetValue(item)?.ToString() == createdTemplate.Name
                    );

                if (newlyCreated != null && !_selectedItems.Contains(newlyCreated))
                {
                    _selectedItems.Add(newlyCreated);
                    PreSelectedItems = [.. _selectedItems];
                    await SelectedItemsChanged.InvokeAsync(_selectedItems);
                    StateHasChanged();
                    Snackbar.Add(string.Format(Resource.SelectionSuccessful, Caption), Severity.Success);
                }
            }
        }
    }
}
