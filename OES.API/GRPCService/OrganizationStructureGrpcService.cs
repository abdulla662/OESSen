using Grpc.Core;
using OES.API.Protos;
using OES.Core.Entities.Schedule;
using OES.Interface.UnitOfWork;

namespace OES.API.GRPCService
{
    public class OrganizationStructureGrpcService : OrganizationStructureTreeService.OrganizationStructureTreeServiceBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrganizationStructureGrpcService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public override async Task<TreeItems> GetOrganizationStructureTreeByRootId(IdCarrier rootId, ServerCallContext context)
        {
            var rootItem = await _unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetByIdAsync(rootId.Id);

            if (rootItem == null)
                return new TreeItems();

            var childrenOfRoot = await _unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetAllAsync(x => x.OrganizationStructureSignature == rootItem.OrganizationStructureSignature);

            var treeItems = childrenOfRoot
                .Select(item => new TreeItem
                {
                    Id = item.Id,
                    Text = item.Name,
                    Description = item.Description ?? "",
                    IsActive = item.IsActive,
                    ParentId = item.ParentId,
                    Signature = item.OrganizationSignature,
                    IsLeaf = item.IsLeaf,
                    IsChildrenLoaded = true,
                    OrganizationId = item.OrganizationId
                })
                .ToList();

            var response = new TreeItems();

            response.Items.AddRange(treeItems);

            return response;
        }
    }
}
