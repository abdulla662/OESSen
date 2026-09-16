namespace OES.Helper.Dtos.ExamServer
{
    public class StageSyncResponseDto
    {
        public long OriginalStageId { get; set; }
        public string Name { get; set; }
        public string? RenderedPartName { get; set; }
        public int Order { get; set; }
        public double TimeInMinutes { get; set; }
        public long PaperFormId { get; set; }
        public string? DecisionPaths { get; set; }
        public string? InstructionSectionTemplate { get; set; }
    }
}
