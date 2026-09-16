
namespace OES.Helper.Dtos.CTRExam
{
    public class SyncJobResponseDto
    {
        public long SyncJobId { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public int CandidateCount { get; set; }
        public double Duration { get; set; }
        public bool IsSuccess { get; set; }
    }
}