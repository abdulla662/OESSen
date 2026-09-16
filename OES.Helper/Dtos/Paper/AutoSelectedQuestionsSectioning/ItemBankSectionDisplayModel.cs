
namespace OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning
{
    public class ItemBankSectionDisplayModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsCollapsed { get; set; }

        public ItemBankSectionDisplayModel() { }

        public ItemBankSectionDisplayModel(long id,
                                           string name,
                                           bool isCollapsed = false)
        {
            Id = id;
            Name = name;
            IsCollapsed = isCollapsed;
        }
    }
}
