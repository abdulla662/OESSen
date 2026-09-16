namespace OES.Helper.Dtos.ExamServer
{
    public class VenueSyncPayload
    {
        public ScheduleSyncResponseDto Schedule { get; set; }
        public List<PaperSyncResponseDto> Papers { get; set; } = [];
        public List<PaperFormSyncResponseDto> Forms { get; set; } = [];
        public List<StageSyncResponseDto> Stages { get; set; } = [];
        public List<SectionSyncResponseDto> Sections { get; set; } = [];
        public List<BlockSyncResponseDto> Blocks { get; set; } = [];
        public List<QuestionSyncResponseDto> Questions { get; set; } = [];
        public List<DisabilitySyncResponseDto> Disabilities { get; set; } = [];
        public List<UserSyncResponseDto> Users { get; set; } = [];
        public List<CandidateDetailsSyncResponseDto> CandidateDetails { get; set; } = [];
        public List<CandidatePaperSyncResponseDto> CandidatePapers { get; set; } = [];
    }
}