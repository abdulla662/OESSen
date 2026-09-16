namespace OES.Helper.Dtos.Form
{
    public sealed record FormMetadataDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;


        public FormMetadataDto() { }

        public FormMetadataDto(
            long id,
            string name,
            string code,
            string description
        )
        {
            Id = id;
            Name = name;
            Code = code;
            Description = description;
        }
    }
}
