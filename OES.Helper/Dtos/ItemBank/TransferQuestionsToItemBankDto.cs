using OES.Helper.Enums;

namespace OES.Helper.Dtos.ItemBank
{
    public sealed record TransferQuestionsToItemBankDto(
        long SourceItemBankId,
        long HostItemBankId,
        ItemBankQuestionsTransferType TransferType
    );
}
