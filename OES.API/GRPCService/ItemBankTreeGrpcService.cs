

using Grpc.Core;
using OES.API.Protos;
using OES.Core.Entities;
using OES.Interface.UnitOfWork;

namespace OES.API.GRPCService
{
    public class ItemBankTreeGrpcService : ItemBankTreeService.ItemBankTreeServiceBase
    {
        private readonly ILogger<ItemBankTreeGrpcService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ItemBankTreeGrpcService(ILogger<ItemBankTreeGrpcService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public override async Task<TreeItems> GetAllNestedItemBanksWithParent(IdCarrier carrier, ServerCallContext context)
        {
            var parentItems = await _unitOfWork
                .Repository<ItemBank, long>()
                .GetAllAsync(item => item.Id == carrier.Id);

            var signatures = parentItems
                .Select(x => x.ItemBankSignature)
                .Distinct()
                .ToList();

            if (signatures.Count == 0)
                return new TreeItems();

            var allItems = await _unitOfWork
                .Repository<ItemBank, long>()
                .GetAllAsync(
                    item => signatures.Contains(item.ItemBankSignature),
                    Including: nameof(ItemBank.ItemBankGroups)
                );

            var data = allItems
                .Select(item =>
                {
                    var treeItem = new TreeItem
                    {
                        Id = item.Id,
                        Text = item.Name,
                        Description = item.Description,
                        Code = item.Code ?? string.Empty,
                        Hours = item.Hours,
                        IsActive = item.IsActive,
                        ParentId = item.ParentId,
                        Signature = item.OrganizationSignature,
                        IsChildrenLoaded = true,
                        LevelId = item.LevelId ?? 0,
                        Unscored = item.Unscored
                    };

                    treeItem.GroupsIds.AddRange(item.ItemBankGroups.Select(x => x.OESGroupId.ToString()));

                    return treeItem;
                })
                .ToList();

            var response = new TreeItems();

            response.Items.AddRange(data);

            return response;
        }

        public override async Task<TreeItems> GetAllowedParentsOfItemBankNode(ParentIdCarrier carrier, ServerCallContext context)
        {
            var response = new TreeItems();

            if (carrier?.ParentId == null)
            {
                return response;
            }

            var startId = Convert.ToInt64(carrier.ParentId);

            var spineIds = new List<long>();

            var spineParentIds = new List<long>();

            var currentId = (long?)startId;

            while (currentId.HasValue)
            {
                var nodes = await _unitOfWork
                    .Repository<ItemBank, long>()
                    .GetAllAsync(x => x.Id == currentId.Value && !x.IsDeleted);

                var node = nodes.FirstOrDefault();

                if (node == null) break;

                spineIds.Add(node.Id);

                if (node.ParentId.HasValue)
                    spineParentIds.Add(node.ParentId.Value);

                currentId = node.ParentId;
            }

            if (spineIds.Count == 0)
                return response;

            var allRelevantNodes = await _unitOfWork
               .Repository<ItemBank, long>()
               .GetAllAsync(x =>
                   !x.IsDeleted &&
                   (spineIds.Contains(x.Id) ||
                   (x.ParentId.HasValue && spineParentIds.Contains(x.ParentId.Value)))
               );

            allRelevantNodes
                .DistinctBy(x => x.Id)
                .ToList()
                .ForEach(node =>
                {
                    response.Items.Add(new TreeItem
                    {
                        Id = node.Id,
                        Text = node.Name,
                        Description = node.Description,
                        Code = node.Code ?? string.Empty,
                        Hours = node.Hours,
                        IsActive = node.IsActive,
                        ParentId = node.ParentId,
                        Signature = node.OrganizationSignature,
                        IsChildrenLoaded = true,
                    });
                });

            return response;
        }
    }
}
