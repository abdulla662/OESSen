using OES.Helper.Interfaces;

namespace OES.Helper.General
{
    public class PaginationSearchModel : IPaginationSearchModel
    {
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public bool PaginationOff { get; set; } = true;

        //Search Parameters
        public string SearchKey { get; set; } = null;
        public bool SearchInName { get; set; } = false;
        public bool SearchInBody { get; set; } = false;
        public bool SearchInDescription { get; set; } = false;
        public DateTime? FromDate { get; set; } = null;
        public DateTime? ToDate { get; set; } = null;
        // TODO: Use enum here or boolean to order by desc or asc, instead of string
        public string OrderBy { get; set; } = null;
        public object? FilterObj { get; set; } = null;

        public PaginationSearchModel GetPaginationModel(int index = 0, int size = 10, bool off = false)
        {
            return new PaginationSearchModel
            {
                PageSize = size,
                PageIndex = index,
                PaginationOff = off,
            };
        }

        public PaginationSearchModel GetPaginationModel(bool off = true)
        {
            return new PaginationSearchModel
            {
                PaginationOff = off,
            };
        }

        public PaginationSearchModel GetPaginationSearchModel(int pageIndex = 0,
                                                              int pageSize = 10,
                                                              string searchKey = null,
                                                              bool searchInName = false,
                                                              bool searchInBody = false,
                                                              bool searchInDescription = false,
                                                              DateTime? fromDate = null,
                                                              DateTime? toDate = null,
                                                              string orderBy = null,
                                                              bool paginationOff = false,
                                                              object filterObject = null)
        {
            return new PaginationSearchModel
            {
                PageSize = pageSize,
                PageIndex = pageIndex,
                SearchKey = searchKey,
                SearchInName = searchInName,
                SearchInBody = searchInBody,
                SearchInDescription = searchInDescription,
                FromDate = fromDate,
                ToDate = toDate,
                OrderBy = orderBy,
                PaginationOff = paginationOff,
                FilterObj = filterObject
            };
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
