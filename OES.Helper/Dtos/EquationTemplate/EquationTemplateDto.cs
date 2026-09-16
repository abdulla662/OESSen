using OES.Helper.Dtos.Paper.Responses;

namespace OES.Helper.Dtos.EquationTemplate
{
    public class EquationDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Equation { get; set; }
        public List<ItemBanksFromItemBankPointResponseDto> SelectedItemBanks { get; set; } = [];
        public bool IsTotalEquation { get; set; }
        public string DisplayEquation { get; set; }
        public bool ShowInResults { get; set; } = true;
    }
}