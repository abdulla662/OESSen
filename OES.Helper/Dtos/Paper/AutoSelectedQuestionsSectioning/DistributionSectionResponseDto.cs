using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning
{
    public class DistributionSectionResponseDto
    {
        public string Name { get; set; }

        public bool IsRestrictedTime { get; set; }

        public double TimeInMinutes { get; set; }

        public bool IsRandom { get; set; }

        public long? InstructionSectionTemplateId { get; set; }

        public int OrderId { get; set; }


        [JsonConstructor]
        public DistributionSectionResponseDto(string name,
                                              bool isRestrictedTime,
                                              double timeInMinutes,
                                              bool isRandom,
                                              long? instructionSectionTemplateId,
                                              int orderId = 1)
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
