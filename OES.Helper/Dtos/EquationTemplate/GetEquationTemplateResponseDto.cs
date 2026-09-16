using SharedHelper.Enums;

namespace OES.Helper.Dtos.EquationTemplate
{
    public class GetEquationTemplateResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string TotalEquation { get; set; }
        public long PaperId { get; set; }
        public string PaperName { get; set; }
        public PaperType PaperType { get; set; }
        public long FormId { get; set; }
        public string FormName { get; set; }
        public List<GetEquationDto> Equations { get; set; } = [];
    }

    public class GetEquationDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Equation { get; set; }
        public List<GetItemBankDto> ItemBanks { get; set; } = [];
        public string DisplayEquation { get; set; }
        public bool ShowInResults { get; set; } = true;
    }

    public class GetItemBankDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}