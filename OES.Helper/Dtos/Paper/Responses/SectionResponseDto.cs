namespace OES.Helper.Dtos.Paper.Responses
{
    public class SectionResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string SectioningIdentifier { get; set; }
        public double TimeInMinutes { get; set; }
        public bool IsRestrictedTime { get; set; }
        public bool IsRandom { get; set; }
        public long? InstructionSectionTemplateId { get; set; }
        public int OrderId { get; set; }
    }
}