namespace OES.Helper.Dtos.ItemBank
{
    public class ItemBankPointsRequestDto
    {
        public long PaperId { get; set; }

        public List<long> ItemBankIds { get; set; }
    }
}
