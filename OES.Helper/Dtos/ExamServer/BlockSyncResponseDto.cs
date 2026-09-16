namespace OES.Helper.Dtos.ExamServer
{
    public class BlockSyncResponseDto
    {
        public long OriginalBlockId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public long DifficultyLevelId { get; set; }
        public long QuestionCategoryId { get; set; }
        public long OriginalSectionId { get; set; } // Points to AdaptiveSectionId in OES
        public long PaperFormId { get; set; }
    }
}
