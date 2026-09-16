using Microsoft.AspNetCore.Components.Forms;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;

namespace OES.Helper.Dtos.AIItemBankGenerator.Request
{
    public sealed record AIItemBankStepperTransferableDto
    {
        public IBrowserFile? File { get; init; }

        public ItemBankLevelsCreation ItemBankLevelsCreation { get; init; }

        public int? MaxLevelsCount { get; init; }

        public int? MaxChildrenPerNode { get; init; }

        public LanguageDto? SelectedLanguageDto { get; init; }
    }
}
