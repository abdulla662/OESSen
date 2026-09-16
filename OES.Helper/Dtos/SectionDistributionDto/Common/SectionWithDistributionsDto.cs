namespace OES.Helper.Dtos.SectionDistributionDto.Common
{
    public class SectionWithDistributionsDto
    {
        public string Name { get; set; }

        public bool IsRestrictedTime { get; set; }

        public double TimeInMinutes { get; set; }

        public bool IsRandom { get; set; }

        public int OrderId { get; set; }

        public long? InstructionSectionTemplateId { get; set; }

        public List<SectionDistributionDto> Distributions { get; set; } = [];
    }
}
