using OES.Helper.Dtos.Section;
using OES.Helper.Dtos.Stage;

namespace OES.Helper.Dtos.Block.Responses
{
    public class PaperStagesBlocksDto
    {
        public long FormId { get; set; }
        public long PaperId { get; set; }
        public List<StageDto> Stages { get; set; } = [];
        public List<GetChoosenBlocksDto> Blocks { get; set; } = [];
        public List<AdaptiveSectionDto> AdaptiveSections { get; set; } = [];
    }
}
