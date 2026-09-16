using OES.Helper.Dtos.SectionDistributionDto.Common;

namespace OES.Helper.Dtos.SectionDistributionDto
{
    public class AddOrUpdateAutoSectionsDistributionsRequestDto
    {
        public long PaperId { get; set; }

        public List<SectionWithDistributionsDto> Sections { get; set; } = [];
    }
}
