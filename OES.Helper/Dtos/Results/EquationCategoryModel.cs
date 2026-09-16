namespace OES.Helper.Dtos.Results
{
    public class EquationCategoryModel
    {
        public string Category { get; set; } = string.Empty;
        public string CategoryEquation { get; set; } = string.Empty;
        public double RawScore { get; set; }
        public double CalculatedValue { get; set; }
    }
}
