using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Services.Interfaces.GRPC
{
    public interface IILoTreeGrpcService
    {
        Task<List<TreeItemResponseDto>> GetAllNestedIlosWithParentAsync(long IdCarrier);

        Task<List<TreeItemResponseDto>> GetAllowedParentsOfIloNodeAsync(long? ParentIdCarrier);
    }
}
