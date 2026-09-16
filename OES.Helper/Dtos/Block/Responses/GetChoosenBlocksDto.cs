using OES.Helper.Dtos.DeltaType;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.QuestionCategory;

namespace OES.Helper.Dtos.Block.Responses
{
    public class GetChoosenBlocksDto
    {
        public long Id { get; set; }
        public long BlockId { get; set; }
        public int QuestionCount { get; set; }
        public string Name { get; set; }
        public long? AdaptiveSectionId { get; set; }
        public string AdaptiveSectionName { get; set; }
        public bool Distribution { get; set; }
        public string Description { get; set; }
        public GetDeltaTypeDto DeltaType { get; set; }
        public QuestionCategoryDto BlockType { get; set; } // Refers to QuestionCategory
        public GetDifficultyLevelDto DifficultyLevel { get; set; }
    }
}
