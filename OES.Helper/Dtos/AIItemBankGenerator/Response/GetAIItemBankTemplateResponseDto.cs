using OES.Helper.Dtos.AIItemBankGenerator.Request;

namespace OES.Helper.Dtos.AIItemBankGenerator.Response
{
    public class GetAIItemBankTemplateResponseDto
    {
        public AIItemBankTemplateCreationDto AIItemBankTemplate { get; set; } = new();
    }
}
