

namespace OES.Helper.Dtos.FlattenedTree.Responses
{
    public class GetItemBankResponseDto
    {
        public long ItemBankId { get; set; }
        public string ItemBankName { get; set; }
        public List<GetQuestionTypeResponseDto> QuestionTypes { get; set; } = [];
    }
}
