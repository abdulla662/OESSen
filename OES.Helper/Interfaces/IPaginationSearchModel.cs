using OES.Helper.General;

namespace OES.Helper.Interfaces
{
    public interface IPaginationSearchModel : IDisposable
    {
        PaginationSearchModel GetPaginationModel(int Pageindex = 0, int Pagesize = 15, bool PaginationOff = false);
        PaginationSearchModel GetPaginationSearchModel(int pageIndex, int pageSize, string searchKey, bool searchInName, bool searchInBody, bool searchInDescription, DateTime? fromDate, DateTime? toDate, string orderBy, bool paginationOff, object filterObject);
        PaginationSearchModel GetPaginationModel(bool off);
    }
}
