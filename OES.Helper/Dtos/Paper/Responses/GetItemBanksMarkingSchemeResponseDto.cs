
namespace OES.Helper.Dtos.Paper.Responses
{
    public class GetItemBanksMarkingSchemeResponseDto
    {
        public long ItemBankId { get; set; }
        public string ItemBankName { get; set; }
        public double ItemBankMark { get; set; } = 0;
        public long QuestionCount { get; set; }
        public double QuestionMark { get; set; }
        public long? FormId { get; set; } // NOTE: This field may be nullable or zero in case of 'Auto' paper.
    }
}
