using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.AIItemBankGenerator.Request
{
    public sealed record AIItemBankGenerationConfigurationsDto
    {
        public required ItemBankLevelsCreation ItemBankLevelsCreation { get; init; }

        public int? MaxLevelsCount { get; init; }

        public int? MaxChildrenPerNode { get; init; }

        public required LanguageDto LanguageDto { get; init; }
    }
}
