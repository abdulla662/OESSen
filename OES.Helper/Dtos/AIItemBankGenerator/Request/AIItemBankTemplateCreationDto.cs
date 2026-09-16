using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.AIItemBankGenerator.Request
{
    public class AIItemBankTemplateCreationDto
    {
        public string Name { get; set; } = string.Empty;

        public bool IsFromAI { get; set; } = true;

        public LanguageDto SelectedLanguage { get; set; } = new();

        public ItemBankLevelsCreation ItemBankLevelsCreation { get; set; }

        public int? MaxLevelsCount { get; set; }

        public int? MaxChildrenPerNode { get; set; }
    }
}
