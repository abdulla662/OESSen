namespace OES.Helper.Dtos.Template.Response
{
    public class UpdateTemplateDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Content { get; set; }

        public long TemplateTypeId { get; set; }
    }
}
