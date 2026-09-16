namespace OES.Helper.Dtos.ExamServer
{
    public class CandidatePaperSyncResponseDto
    {
        public long CandidateId { get; set; }

        public long SchedulePaperId { get; set; }

        public long PaperFormId { get; set; }

        public long RegistrationNumber { get; set; }

        public DateTime? CandidateExamDate { get; set; }

        public long VenueId { get; set; }

        public List<long>? AccumulatedSolvedBlockIds { get; set; }

        public float? CandidateExtraTimePercent { get; set; }
    }
}