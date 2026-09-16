using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.AIItemBankGenerator.Request
{
    public sealed record AIItemBankGenerationPromptContextDto
    {
        public required string DocumentText { get; init; }

        public required ItemBankLevelsCreation ItemBankLevelsCreation { get; init; }

        public int? MaxLevelsCount { get; init; }

        public int? MaxChildrenPerNode { get; init; }

        public required LanguageDto LanguageDto { get; init; }

        public List<ExistingLevelOptionDto> ExistingLevels { get; init; } = [];
    }

    public sealed record ExistingLevelOptionDto
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}