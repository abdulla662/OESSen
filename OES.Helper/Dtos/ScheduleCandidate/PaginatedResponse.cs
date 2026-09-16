namespace OES.Helper.Dtos.ScheduleCandidate
{
    public sealed class PaginatedResponse<T>
    {
        public List<T> Items { get; set; }

        public int TotalItems { get; set; }
    }
}
