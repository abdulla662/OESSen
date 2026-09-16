
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning
{
    public class DistributionSectionRequestDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameisRequierd))]
        public string Name { get; set; }

        public bool IsRestrictedTime { get; set; }

        [Required]
        public double TimeInMinutes { get; set; }

        public bool IsRandom { get; set; }

        public int OrderId { get; set; } = 1;


        // Additional properties for section management

        public bool IsRenaming { get; set; }

        public string NewName { get; set; } = string.Empty;

        public bool IsCollapsed { get; set; }

        public long? InstructionSectionTemplateId { get; set; }

        public DistributionSectionRequestDto() { }

        public DistributionSectionRequestDto(string name)
        {
            Name = name;
        }

        public DistributionSectionRequestDto(string name,
                                             bool isRestrictedTime,
                                             double timeInMinutes,
                                             bool isRandom,
                                             long? instructionSectionTemplateId,
                                             int orderId)
        {
            Name = name;
            IsRestrictedTime = isRestrictedTime;
            TimeInMinutes = timeInMinutes;
            IsRandom = isRandom;
            InstructionSectionTemplateId = instructionSectionTemplateId;
            OrderId = orderId;
        }
    }
}
