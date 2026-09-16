using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Common
{
    public interface IBlazGetCustomTableData<T>
    {
        public Task<CustomTableData<T>> GetCustomTableData(PaginationSearchModel paginationSearchModel, string apiUrl);
    }
}
