using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.Dtos;
namespace OES.Blazor.Components.Common
{
    public partial class NormalBtnCRUD<T>
    {
        [Inject] CRUD_Dto _cRUD_Dto { get; set; }
        [Inject] NavigationManager _navigationManager { set; get; }
        [Inject] IBlazSessionStorageService _SessoinStorage { set; get; }

        [Parameter] public bool? CRUD_Info_Btn { get; set; }
        [Parameter] public string? Info_URL { get; set; }

        /// <summary>
        /// Allow Edit button
        /// </summary>
        [Parameter] public bool CRUD_Edit_Btn { get; set; }
        [Parameter] public bool CRUD_Edit_Disabled { get; set; }
        [Parameter] public string? Edit_URL { get; set; }

        /// <summary>
        /// Allow Delete button
        /// </summary>
        [Parameter] public bool CRUD_Delete_Btn { get; set; }
        [Parameter] public bool CRUD_Delete_Disabled { get; set; }
        [Parameter] public string? Delete_URL { get; set; }

        /// <summary>
        /// Allow View button
        /// </summary>
        [Parameter] public bool? CRUD_View_Btn { get; set; }
        [Parameter] public string? View_URL { get; set; }

        /// <summary>
        /// Allow Add button
        /// </summary>
        [Parameter] public bool? CRUD_Add_Btn { get; set; }
        [Parameter] public string? Add_URL { get; set; }

        // Search Parameters
        [Parameter] public string? AddNew_URL { get; set; }
        [Parameter] public EventCallback<object> EditClick_fun { get; set; }
        [Parameter] public EventCallback<object> DeleteClick_fun { get; set; }
        [Parameter] public EventCallback<object> ViewClick_fun { get; set; }
        [Parameter] public EventCallback<object> InfoClick_fun { get; set; }
        [Parameter] public EventCallback<object> AddClick_fun { get; set; }
        [Parameter] public object ObjectId { get; set; }
        [Parameter] public EventCallback<T> OnRowSelected { get; set; }
        [Parameter] public T? Item { get; set; }
        [Parameter] public RenderFragment<object>? ButtonChildContent { get; set; }


        public async Task PerformEditBtnClick(object Id)
        {
            _cRUD_Dto.Id = Id;
            _cRUD_Dto.IsCreatePage = false;

            await SelectRow(Item);

            await _SessoinStorage.SetValue("IsCreatePage", _cRUD_Dto.IsCreatePage);
            await _SessoinStorage.SetValue("PerformEditBtnClick", Id.ToString());

            if (EditClick_fun.HasDelegate)
            {
                // Invoke the delegate asynchronously
                await EditClick_fun.InvokeAsync(Id);
            }
            else
            {
                // If no delegate is attached, navigate to a specific URL
                _navigationManager.NavigateTo((Edit_URL is not null ? Edit_URL : "Edit/"));
            }
        }

        public async Task PerformDeleteBtnClick(object Id)
        {
            _cRUD_Dto.Id = Id;
            _cRUD_Dto.IsCreatePage = false;

            await SelectRow(Item);

            await _SessoinStorage.SetValue("IsCreatePage", _cRUD_Dto.IsCreatePage);
            await _SessoinStorage.SetValue("PerformDeleteBtnClick", Id.ToString());

            if (DeleteClick_fun.HasDelegate)
            {
                // Invoke the delegate asynchronously
                await DeleteClick_fun.InvokeAsync(Id);
            }
            else
            {
                _navigationManager.NavigateTo((Delete_URL is not null ? Delete_URL : "Delete/"));
            }
        }

        public async Task PerformViewBtnClick(object Id)
        {
            _cRUD_Dto.Id = Id;
            _cRUD_Dto.IsCreatePage = false;

            await SelectRow(Item);

            await _SessoinStorage.SetValue("IsCreatePage", _cRUD_Dto.IsCreatePage);
            await _SessoinStorage.SetValue("PerformViewBtnClick", Id.ToString());

            if (ViewClick_fun.HasDelegate)
            {
                // Invoke the delegate asynchronously
                await ViewClick_fun.InvokeAsync(Id);
            }
            else
            {
                _navigationManager.NavigateTo((View_URL is not null ? View_URL : "View/"));
            }
        }

        public async Task PerformAddBtnClick(object Id)
        {
            _cRUD_Dto.Id = Id;
            _cRUD_Dto.IsCreatePage = false;

            await SelectRow(Item);

            await _SessoinStorage.SetValue("IsCreatePage", _cRUD_Dto.IsCreatePage);
            await _SessoinStorage.SetValue("PerformAddBtnClick", Id.ToString());

            if (AddClick_fun.HasDelegate)
            {
                // Invoke the delegate asynchronously
                await AddClick_fun.InvokeAsync(Id);
            }
            else
            {
                _navigationManager.NavigateTo((Add_URL is not null ? Add_URL : "Add/"));
            }
        }

        public async Task PerformInfoBtnClick(object Id)
        {
            _cRUD_Dto.Id = Id;
            _cRUD_Dto.IsCreatePage = false;

            await SelectRow(Item);

            await _SessoinStorage.SetValue("IsCreatePage", _cRUD_Dto.IsCreatePage);
            await _SessoinStorage.SetValue("PerformInfoBtnClick", Id.ToString());

            if (InfoClick_fun.HasDelegate)
            {
                // Invoke the delegate asynchronously
                await InfoClick_fun.InvokeAsync(Id);
            }
            else
            {
                _navigationManager.NavigateTo((Info_URL is not null ? Info_URL : "Info/"));
            }
        }

        private async Task SelectRow(T item)
        {
            await OnRowSelected.InvokeAsync(item);
        }
    }
}