using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Services.Interfaces.GRPC
{
    public interface IItemBankTreeGrpcService
    {
        Task<List<TreeItemResponseDto>> GetAllNestedItemBanksWithParentAsync(long IdCarrier);
        Task<List<TreeItemResponseDto>> GetAllowedParentsOfItemBankNodeAsync(long? ParentIdCarrier);

    }
}
