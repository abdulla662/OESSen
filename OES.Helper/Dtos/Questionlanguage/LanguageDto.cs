using OES.Helper.ResourceFiles;

namespace OES.Helper.Dtos.Questionlanguage
{
    public class LanguageDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string LanguageDirection { get; set; }

        public string Direction => Resource.PropertyLocalization(LanguageDirection);

        public string CreationUser { get; set; }
    }
}
