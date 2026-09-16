using OES.Blazor.Protos;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Helper.Dtos.TreeItem;

namespace OES.Blazor.Services.Implementation.GRPC
{
    public class ILoTreeGrpcService : IILoTreeGrpcService
    {
        private readonly IloTreeService.IloTreeServiceClient _client;

        public ILoTreeGrpcService(IloTreeService.IloTreeServiceClient client)
        {
            _client = client;
        }

        public async Task<List<TreeItemResponseDto>> GetAllNestedIlosWithParentAsync(long IdCarrier)
        {
            var request = new IdCarrier
            {
                Id = IdCarrier
            };

            var response = await _client.GetAllNestedIlosWithParentAsync(request);

            return response.Items.Select(MapToTreeItemResponseDto).ToList();
        }

        public async Task<List<TreeItemResponseDto>> GetAllowedParentsOfIloNodeAsync(long? ParentIdCarrier)
        {
            var request = new ParentIdCarrier
            {
                ParentId = ParentIdCarrier
            };

            var response = await _client.GetAllowedParentsOfIloNodeAsync(request);

            return response.Items.Select(MapToTreeItemResponseDto).ToList();
        }

        private TreeItemResponseDto MapToTreeItemResponseDto(TreeItem node)
        {
            return new TreeItemResponseDto
            {
                Id = node.Id,
                Text = node.Text,
                ParentId = node.ParentId,
                Description = node.Description,
                Code = node.Code,
                IsActive = node.IsActive,
                Signature = node.Signature,
                Children = [],
                IsChildrenLoaded = node.IsChildrenLoaded,
                OrganizationId = node.OrganizationId,
                GroupsIds = node.GroupsIds.Select(Guid.Parse).ToList(),
            };
        }
    }
}
