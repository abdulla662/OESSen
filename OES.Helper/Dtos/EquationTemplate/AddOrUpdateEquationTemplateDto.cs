namespace OES.Helper.Dtos.EquationTemplate
{
    public class AddOrUpdateEquationTemplateDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long PaperId { get; set; }
        public long FormId { get; set; }
        public string TotalEquation { get; set; }
        public List<EquationItemDto> Equations { get; set; } = [];
    }

    public class EquationItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Equation { get; set; }
        public List<long> ItemBankIds { get; set; } = [];
        public bool ShowInResults { get; set; } = true;
    }
}