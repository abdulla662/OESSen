namespace OES.Helper.Dtos.ExamServer
{
    public class SectionSyncResponseDto
    {
        public long OriginalSectionId { get; set; }
        public string Name { get; set; }
        public int OrderId { get; set; }
        public double TimeInMinutes { get; set; }
        public string Type { get; set; }
        public string AdaptivePaperSubtype { get; set; }
        public long PaperFormId { get; set; }
        public long? StageId { get; set; }

        // Standard-specific
        public bool IsRestrictedTime { get; set; }
        public bool IsRandom { get; set; }

        // Adaptive-specific
        public bool UnScored { get; set; }
        public long? DifficultyLevelId { get; set; }
        public string? InstructionSectionTemplate { get; set; }
    }
}
