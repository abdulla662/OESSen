using System.Text.Json.Serialization;

namespace OES.Helper.General.DocLibPaginatedList
{
    public class DocLibPaginatedList<T>
    {
        public List<T> Items { get; }
        public int PageNumber { get; }
        public int TotalPages { get; }
        public int TotalCount { get; }

        [JsonConstructor]
        public DocLibPaginatedList(List<T> items, int pageNumber, int totalPages, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            TotalPages = totalPages;
            TotalCount = totalCount;
        }

        [JsonIgnore]
        public bool HasPreviousPage => PageNumber > 1;

        [JsonIgnore]
        public bool HasNextPage => PageNumber < TotalPages;

        [JsonIgnore]
        public static DocLibPaginatedList<T> Empty => new DocLibPaginatedList<T>([], 0, 0, 0);

        public static DocLibPaginatedList<T> Concat(DocLibPaginatedList<T> firstList, DocLibPaginatedList<T> secondList)
        {
            var items = firstList.Items.Concat(secondList.Items).ToList();

            var count = firstList.TotalCount + secondList.TotalCount;

            int pageNumber = firstList.PageNumber;

            int pageSize = firstList.Items.Count + secondList.Items.Count;

            return new DocLibPaginatedList<T>(items, count, pageNumber, pageSize);
        }
    }
}
