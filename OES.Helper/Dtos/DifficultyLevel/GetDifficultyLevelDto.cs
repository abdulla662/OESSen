
namespace OES.Helper.Dtos.DifficultyLevel
{
    public class GetDifficultyLevelDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal FromDelta { get; set; }
        public decimal ToDelta { get; set; }
        public long DeltaTypeId { get; set; }
    }
}
