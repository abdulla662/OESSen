using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Paper.Requests
{
    public class SectionRequestDto
    {
        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ThisFieldIsRequired))]
        public string Name { get; set; }

        public string SectioningIdentifier { get; set; }

        public bool IsRestrictedTime { get; set; }

        public double TimeInMinutes { get; set; }

        public bool IsRandom { get; set; }

        public int OrderId { get; set; }


        // Additional properties for section management

        public bool IsRenaming { get; set; }

        public string NewName { get; set; }

        public long? InstructionSectionTemplateId { get; set; }

        public SectionRequestDto() { }

        public SectionRequestDto(string name, string sectioningIdentifier, int orderId = 0)
        {
            Name = name;
            NewName = name;
            SectioningIdentifier = sectioningIdentifier;
            OrderId = orderId;
        }

        public SectionRequestDto(string name,
                                 string sectioningIdentifier,
                                 bool isRestrictedTime,
                                 double timeInMinutes,
                                 bool isRandom,
                                 long? instructionSectionTemplateId,
                                 int orderId = 0)
        {
            Name = name;
            SectioningIdentifier = sectioningIdentifier;
            IsRestrictedTime = isRestrictedTime;
            TimeInMinutes = timeInMinutes;
            IsRandom = isRandom;
            InstructionSectionTemplateId = instructionSectionTemplateId;
            OrderId = orderId;
        }
    }
}