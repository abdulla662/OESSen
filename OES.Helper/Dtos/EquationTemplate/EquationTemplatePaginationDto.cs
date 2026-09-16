namespace OES.Helper.Dtos.EquationTemplate
{
    public class EquationTemplatePaginationDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public int EquationCount { get; set; }

        public string CategoryNames { get; set; }

        public DateTime CreationDate { get; set; }
    }
}
