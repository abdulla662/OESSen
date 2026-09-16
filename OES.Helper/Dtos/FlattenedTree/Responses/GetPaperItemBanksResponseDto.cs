
namespace OES.Helper.Dtos.FlattenedTree.Responses
{
    public class GetPaperItemBanksResponseDto
    {
        public long PaperId { get; set; }

        public List<GetItemBankResponseDto> ItemBanks { get; set; } = [];
    }
}
