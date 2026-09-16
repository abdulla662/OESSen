namespace OES.Helper.Dtos.Form
{
    public class QuestionWithFormsDto
    {
        public string QuestionCode { get; set; } = string.Empty;

        public List<FormListDto> Forms { get; set; } = [];
    }
}
