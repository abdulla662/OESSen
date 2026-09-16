
using SharedHelper.Enums;

namespace OES.Helper.Dtos.Section
{
    public class AdaptiveSectionDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public AdaptivePaperSubtype AdaptivePaperSubtype { get; set; }
        public int Order { get; set; }
        public long StageId { get; set; }
        public bool UnScored { get; set; }
        public long? InstructionSectionAdaptiveId { get; set; }
        public string? InstructionSectionAdaptiveName { get; set; }
        public double TimeInMinutes { get; set; }
        public long DifficultyLevelId { get; set; }
    }
}
