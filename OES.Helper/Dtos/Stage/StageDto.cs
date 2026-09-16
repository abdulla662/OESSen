using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Stage
{
    public class StageDto
    {
        public long Id { get; set; }

        public long PaperId { get; set; }

        public int Order { get; set; }

        public string Name { get; set; }

        [Required]
        public string? RenderedPartName { get; set; }

        public long? InstructionSectionAdaptiveId { get; set; }

        public string? InstructionSectionAdaptiveName { get; set; }

        [Required]
        public double TimeInMinutes { get; set; }
    }
}
