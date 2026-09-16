using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.Interfaces;
using OES.Helper.RegularExpressions;
using OES.Helper.ResourceFiles;
using SharedHelper.RolesNames;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OES.Blazor.Components.Common
{
    public partial class ListItem<T> : ComponentBase
    {
        [Inject] IPaginationSearchModel PaginationSearchModel { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] NavigationManager _navigationManager { get; set; }
        [Inject] GlobalUserContext GlobalUserContext { get; set; }
        [Inject] CRUD_Dto CrudDto { get; set; }

        [Parameter] public bool MultiSelection { get; set; } = false;
        [Parameter] public string? Caption { get; set; }
        [Parameter] public string CaptionIcon { get; set; }
        [Parameter] public string? ListTitle { get; set; }
        [Parameter] public string[]? DisplayColumns { get; set; }
        [Parameter] public string[]? ExcludedColumns { get; set; }
        [Parameter] public EndPointDel? EndpointServerice { get; set; }
        [Parameter] public bool CRUD_Info_Btn { get; set; }
        [Parameter] public string? Info_URL { get; set; }
        [Parameter] public bool CRUD_Edit_Btn { get; set; }
        [Parameter] public bool CRUD_Edit_Disabled { get; set; }
        [Parameter] public string? Edit_URL { get; set; }
        [Parameter] public bool CRUD_Delete_Btn { get; set; }
        [Parameter] public bool CRUD_Delete_Disabled { get; set; }
        [Parameter] public string? Delete_URL { get; set; }
        [Parameter] public bool CRUD_View_Btn { get; set; }
        [Parameter] public string? View_URL { get; set; }
        [Parameter] public bool ShowActionsColumn { get; set; }
        [Parameter] public bool CRUD_Add_Btn { get; set; }
        [Parameter] public string Add_URL { get; set; }
        [Parameter] public string? AddNew_URL { get; set; }
        [Parameter] public List<string> AddNew_URL_OesRolesList { get; set; } = [];
        [Parameter] public List<string> CreateClick_fun_OesRolesList { get; set; } = [];
        [Parameter] public bool? Create_Btn { get; set; } = false;
        [Parameter] public bool Hide_DescriptionSearch { get; set; }
        [Parameter] public bool HideSearchInBody { get; set; } = true;
        [Parameter] public bool HideSearchInCode { get; set; } = true;
        [Parameter] public bool HideCodeSearch { get; set; } = false;
        [Parameter] public bool HideDateRangeFilter { get; set; } = false;
        [Parameter] public bool HideOrderByFilter { get; set; } = false;
        [Parameter] public string FirstSearchParameterLabel { get; set; }
        [Parameter] public string SecondSearchParameterLabel { get; set; }
        [Parameter] public string ThirdSearchParameterLabel { get; set; }
        [Parameter] public Func<string, bool, Task> OnToggle { get; set; }
        [Parameter] public bool ShowIsActiveColumn { get; set; }
        [Parameter] public Func<string, bool, Task> CustomToggleFunction { get; set; }
        [Parameter] public EventCallback<object> EditClick_fun { get; set; }
        [Parameter] public EventCallback<object> DeleteClick_fun { get; set; }
        [Parameter] public EventCallback<object> CreateClick_fun { get; set; }
        [Parameter] public EventCallback<object> ViewClick_fun { get; set; }
        [Parameter] public EventCallback<object> InfoClick_fun { get; set; }
        [Parameter] public EventCallback<object> AddClick_fun { get; set; }
        [Parameter] public Func<T, bool> EditDisabledPredicate { get; set; }
        [Parameter] public Func<T, bool> DeleteDisabledPredicate { get; set; }
        [Parameter] public EventCallback<T> SelectedItemChanged { get; set; }
        [Parameter] public EventCallback<HashSet<T>> SelectedItemsChanged { get; set; }
        [Parameter] public EventCallback<T> OnRowSelected { get; set; }
        [Parameter] public RenderFragment<object>? ButtonChildContent { get; set; }
        [Parameter] public RenderFragment? CustomTopButton { get; set; }
        [Parameter] public RenderFragment? TopChildContent { get; set; }
        [Parameter] public RenderFragment? BottomChildContent { get; set; }
        [Parameter] public object FilterObject { get; set; }
        [Parameter] public bool SearchPanelVisible { get; set; } = true;
        [Parameter] public HashSet<T> SelectedItems { get; set; } = [];
        [Parameter] public int MaxDisplayLength { get; set; } = 30;
        [Parameter] public string TableHeight { get; set; } = "calc(100vh - 300px)";
        [Parameter] public bool ShowSelectAllInActionsColumn { get; set; } = false;
        [Parameter] public string[]? NoConvertColumns { get; set; }
        [Parameter] public bool ShowCheckBox { get; set; } = false;
        [Parameter] public HashSet<T> LockedItems { get; set; } = [];
        [Parameter] public string LockedItemIcon { get; set; } = "";

        private T SelectedItem { get; set; } = default(T);
        public string SearchKey { get; set; }
        public bool SearchInName { get; set; } = true;
        public bool SearchInBody { get; set; } = false;
        public bool SearchInDescription { get; set; }
        public DateRange DateRange { get; set; }
        public string OrderBy { get; set; } = SearchInKey.DESC;
        public DateTime? FromDate => DateRange?.Start;
        public DateTime? ToDate => DateRange?.End;

        private bool IsCurrentUserSuperAdminOrEntityAdmin =>
            GlobalUserContext.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin || x.Name == AdminRoles.Entity_Admin);


        private Regex _mediaTagsRegex = new Regex(RegularExpressions.MediaTagsPattern, RegexOptions.IgnoreCase);

        public MudTable<T> table;

        PropertyInfo[] Properties;

        public delegate Task<CustomTableData<T>> EndPointDel(PaginationSearchModel pagination);

        private Dictionary<T, bool> ActiveStates = new Dictionary<T, bool>();

        private int rowsPerPage = 10;


        protected override void OnParametersSet()
        {
            if (DisplayColumns != null && ExcludedColumns != null && DisplayColumns.Length > 0 && ExcludedColumns.Length > 0)
            {
                throw new InvalidOperationException(Resource.CannotCombineIncludedAndExcludedColumns);
            }

            if (typeof(T) != null)
            {
                Properties = GetProperties(typeof(T), DisplayColumns, ExcludedColumns);
            }
        }

        PropertyInfo[] GetProperties(Type type, string[]? includedColumns, string[]? excludedColumns)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (includedColumns != null && includedColumns.Any())
            {
                return includedColumns.Select(column => type.GetProperty(column, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance))
                                      .Where(property => property != null)
                                      .ToArray();
            }
            else if (excludedColumns != null && excludedColumns.Any())
            {
                return type.GetProperties()
                           .Where(property => !excludedColumns.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                           .ToArray();
            }
            else
            {
                return type.GetProperties();
            }
        }

        string GetDisplayName(PropertyInfo property)
        {
            var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>();
            return displayNameAttribute is not null ? displayNameAttribute.DisplayName : property.Name;
        }

        private async Task<TableData<T>> GetDataService(TableState state, CancellationToken cancellationToken)
        {
            var paginationSearchModel = await EndpointServerice(PaginationSearchModel.GetPaginationSearchModel(
                pageIndex: state.Page,
                pageSize: state.PageSize,
                searchKey: SearchKey,
                searchInName: SearchInName,
                searchInBody: SearchInBody,
                searchInDescription: SearchInDescription,
                fromDate: FromDate,
                toDate: ToDate,
                orderBy: OrderBy,
                paginationOff: false,
                filterObject: FilterObject
            ));

            return new TableData<T>()
            {
                TotalItems = paginationSearchModel.TotalItems,
                Items = paginationSearchModel.Items,
            };
        }

        private void OnTableSearch()
        {
            table.ReloadServerData();
        }

        private void OnResetSearch()
        {
            SearchKey = string.Empty;
            SearchInName = true;
            SearchInDescription = false;
            DateRange = new DateRange(null, null);
            OrderBy = SearchInKey.DESC;
            table.ReloadServerData();
            StateHasChanged();
        }

        private void CheckTextCleared(string? value)
        {
            if (string.IsNullOrEmpty(value?.ToString()))
            {
                OnTableSearch();
            }
        }

        public async Task PerformAddBtnClick()
        {
            CrudDto.Id = 0;
            CrudDto.IsCreatePage = true;
            await BlazSessionStorageService.SetValue("IsCreatePage", CrudDto.IsCreatePage);
            _navigationManager.NavigateTo(AddNew_URL);
        }

        private async Task Toggle_fun(T item, bool value)
        {
            ActiveStates[item] = value;

            await Task.CompletedTask;
        }

        private string GetRowClassFunc(T item, int index)
        {
            var classes = "";

            if (!object.Equals(SelectedItem, default(T)) && item.Equals(SelectedItem))
                classes += "selected-row ";

            if (LockedItems.Contains(item))
                classes += "locked-row ";

            return classes.TrimEnd();
        }

        private async Task HandleRowSelected(T item)
        {
            SelectedItem = item;

            if (OnRowSelected.HasDelegate)
            {
                await OnRowSelected.InvokeAsync(item);
            }

            StateHasChanged();
        }

        public async Task PerformCreateBtnClick()
        {
            CrudDto.Id = 0;
            CrudDto.IsCreatePage = true;
            await BlazSessionStorageService.SetValue("IsCreatePage", CrudDto.IsCreatePage);

            if (CreateClick_fun.HasDelegate)
            {
                // Invoke the delegate asynchronously
                await CreateClick_fun.InvokeAsync();
            }
            else
            {
                _navigationManager.NavigateTo(Add_URL is not null ? Add_URL : "Add/");
            }
        }

        private async Task SelectedItemChangedAsync(T item)
        {
            SelectedItem = item;

            await SelectedItemChanged.InvokeAsync(item);
        }

        private async Task SelectedItemsChangedAsync(HashSet<T> items)
        {
            items.UnionWith(LockedItems);

            SelectedItems = items;

            await SelectedItemsChanged.InvokeAsync(items);
        }

        private string ProcessMediaTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return _mediaTagsRegex.Replace(input, match =>
            {
                var tagName = match.Groups[1].Value.ToLower();

                if (tagName == "audio")
                {
                    var newId = Guid.NewGuid().ToString();

                    return match.Value.Insert(match.Value.IndexOf('>'), $" id='{newId}'");
                }

                return match.Value;
            });
        }

        private async Task SelectAllCurrentPageItems()
        {
            if (table?.FilteredItems != null)
            {
                var currentItems = table.FilteredItems.Take(table.RowsPerPage);
                SelectedItems.UnionWith(currentItems);
                await SelectedItemsChanged.InvokeAsync(SelectedItems);
                StateHasChanged();
            }
        }

        private async Task DeselectAllCurrentPageItems()
        {
            if (table?.FilteredItems != null)
            {
                var currentItems = table.FilteredItems.Take(table.RowsPerPage);
                SelectedItems.ExceptWith(currentItems);
                await SelectedItemsChanged.InvokeAsync(SelectedItems);
                StateHasChanged();
            }
        }

        private bool AreAllCurrentPageItemsSelected()
        {
            if (table?.FilteredItems == null || !table.FilteredItems.Any())
                return false;

            var currentItems = table.FilteredItems.Take(table.RowsPerPage);
            return currentItems.All(item => SelectedItems.Contains(item));
        }

        private bool IsEditDisabled(T item)
        {
            return CRUD_Edit_Disabled || (EditDisabledPredicate?.Invoke(item) ?? false);
        }

        private bool IsDeleteDisabled(T item)
        {
            return CRUD_Delete_Disabled || (DeleteDisabledPredicate?.Invoke(item) ?? false);
        }

        private async Task ToggleSelectionAsync(T item, bool value)
        {
            if (value)
                SelectedItems.Add(item);
            else
                SelectedItems.Remove(item);

            await SelectedItemsChanged.InvokeAsync(SelectedItems);

            StateHasChanged();
        }

        private async Task ToggleSelectAllAsync(bool selectAll)
        {
            if (selectAll && table?.Items != null)
                SelectedItems = [.. table.Items];
            else
                SelectedItems.Clear();

            await SelectedItemsChanged.InvokeAsync(SelectedItems);

            StateHasChanged();
        }
    }
}
