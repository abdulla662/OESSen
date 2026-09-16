using OES.Blazor.Protos;
using OES.Blazor.Services.Interfaces.GRPC;
using OES.Helper.Dtos.TreeItem;


namespace OES.Blazor.Services.Implementation.GRPC
{
    public class ItemBankTreeGrpcService : IItemBankTreeGrpcService
    {
        private readonly ItemBankTreeService.ItemBankTreeServiceClient _client;

        public ItemBankTreeGrpcService(ItemBankTreeService.ItemBankTreeServiceClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));

        }

        public async Task<List<TreeItemResponseDto>> GetAllNestedItemBanksWithParentAsync(long IdCarrier)
        {
            var request = new IdCarrier
            {
                Id = IdCarrier
            };

            var response = await _client.GetAllNestedItemBanksWithParentAsync(request);

            return [.. response.Items.Select(MapToTreeItemResponseDto)];
        }

        public async Task<List<TreeItemResponseDto>> GetAllowedParentsOfItemBankNodeAsync(long? ParentIdCarrier)
        {
            var request = new ParentIdCarrier
            {
                ParentId = ParentIdCarrier
            };

            var response = await _client.GetAllowedParentsOfItemBankNodeAsync(request);

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
                Hours = node.Hours,
                IsActive = node.IsActive,
                Signature = node.Signature,
                Children = [],
                IsChildrenLoaded = node.IsChildrenLoaded,
                GroupsIds = node.GroupsIds.Select(Guid.Parse).ToList(),
                LevelId = node.LevelId,
                Unscored = node.Unscored,
            };
        }
    }
}
