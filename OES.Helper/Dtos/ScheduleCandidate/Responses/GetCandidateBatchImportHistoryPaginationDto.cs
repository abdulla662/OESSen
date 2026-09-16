using OES.Helper.Enums;

namespace OES.Helper.Dtos.ScheduleCandidate.Responses
{
    public class GetCandidateBatchImportHistoryPaginationDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public Guid FileId { get; set; }

        public long SchedulePaperId { get; set; }

        public string CreationUser { get; set; }

        public DateTime CreationDate { get; set; }

        public bool IsReversed { get; set; }

        public DataSource DataSource { get; set; }
    }
}
