
namespace OES.Helper.Dtos.Block.Responses
{
    public class BlockResponseDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public long DeltaTypeId { get; set; }

        public string DeltaTypeName { get; set; }

        public long DifficultyLevelId { get; set; }

        public string DifficultyLevelName { get; set; }

        public string TypeName { get; set; }
    }
}
