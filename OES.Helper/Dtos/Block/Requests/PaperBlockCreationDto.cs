using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.Block.Requests
{
    public class PaperBlockCreationDto
    {
        public long PaperId { get; set; }

        public List<long> SelectedBlocksIds { get; set; }


        public PaperBlockCreationDto() { }

        [JsonConstructor]
        public PaperBlockCreationDto(long paperId, List<long> selectedBlocksIds)
        {
            PaperId = paperId;
            SelectedBlocksIds = selectedBlocksIds;
        }
    }
}
