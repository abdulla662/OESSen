using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Pages.RuleMatrix;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Components.GenericComponents.GroupRolesSelectionDialogs
{
    public partial class GenericGroupSelectionDialog<TEntity>
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] public IDialogService DialogService { get; set; } = default!;

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public Func<PaginationSearchModel, Task<CustomTableData<TEntity>>> EndpointService { get; set; }
        [Parameter] public string[] DisplayColumns { get; set; }
        [Parameter] public string Caption { get; set; }
        [Parameter] public string[] Title { get; set; }
        [Parameter] public bool DeleteBtn { get; set; } = true;
        [Parameter] public string AddNewUrl { get; set; }
        [Parameter] public List<string> AddNewRolesList { get; set; }
        [Parameter] public string ViewUrl { get; set; }
        [Parameter] public EventCallback<object> OnEdit { get; set; }
        [Parameter] public EventCallback<object> OnDelete { get; set; }
        [Parameter] public ResourceType ResourceType { get; set; } = ResourceType.All;
        [Parameter] public EventCallback<Guid> OnConfirmDelete { get; set; }
        [Parameter] public List<TEntity> PreSelectedItems { get; set; } = [];

        private ListItem<TEntity> listRef;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender && listRef != null && PreSelectedItems?.Any() == true)
            {
                listRef.SelectedItems = [.. PreSelectedItems];

                StateHasChanged();
            }
        }

        private void SaveSelection()
        {
            var items = listRef.SelectedItems
                .GroupBy(x =>
                {
                    var prop = typeof(TEntity).GetProperty("Id");
                    return prop.GetValue(x);
                })
                .Select(g => g.First())
                .ToList();

            MudDialog.Close(DialogResult.Ok(items));
        }

        private async Task HandleDeleteClick(object entityIdObject)
        {
            if (entityIdObject == null)
                return;

            Guid entityId = entityIdObject is Guid guidValue
                ? guidValue
                : Guid.Parse(entityIdObject.ToString()!);

            var parameters = new DialogParameters
            {
                { nameof(GenericDialog.Title), Resource.ConfirmDelete },
                { nameof(GenericDialog.Content), Resource.ConfirmDeleteMessage },
                { nameof(GenericDialog.SubmitText), Resource.Delete },
                { nameof(GenericDialog.SubmitButtonColor), Color.Error },
                { nameof(GenericDialog.SubmitButtonStartIcon), Icons.Material.Filled.Delete },
                { nameof(GenericDialog.CancelText), Resource.Cancel },
                { nameof(GenericDialog.CancelButtonStartIcon), Icons.Material.Filled.Close }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                CloseOnEscapeKey = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.ConfirmDelete, parameters, options);
            var result = await dialog.Result;

            if (result.Canceled)
                return;

            if (OnConfirmDelete.HasDelegate)
            {
                await OnConfirmDelete.InvokeAsync(entityId);

                if (listRef != null)
                    await listRef.table.ReloadServerData();
            }
        }

        private async Task OpenEditTemplateDialog(Guid templateId)
        {
            var parameters = new DialogParameters
            {
                { DialogParameterKeys.TemplateId, templateId },
                { DialogParameterKeys.ResourceType, ResourceType }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            };

            var dialog = await DialogService.ShowAsync<EditRoleTemplate>(Resource.EditTemplate, parameters, options);

            var result = await dialog.Result;

            if (result.Canceled)
            {
                Snackbar.Add(Resource.ActionCanceled, Severity.Info);
            }
        }

        private Task<CustomTableData<TEntity>> FetchDataAsync(PaginationSearchModel pagination)
            => EndpointService.Invoke(pagination);

        private async Task OnEditClickAsync(object templateId)
            => await OpenEditTemplateDialog((Guid)templateId);
    }
}
