
using Grpc.Core;
using OES.API.Protos;
using OES.Core.Entities;
using OES.Interface.UnitOfWork;

namespace OES.API.GRPCService
{
    public class IloTreeGrpcService : IloTreeService.IloTreeServiceBase
    {
        private readonly ILogger<IloTreeGrpcService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public IloTreeGrpcService(ILogger<IloTreeGrpcService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public override async Task<TreeItems> GetAllNestedIlosWithParent(IdCarrier carrier, ServerCallContext context)
        {
            var data = (await _unitOfWork
                .Repository<ILO, long>()
                .GetAllAsync(item => item.Id == carrier.Id, Including: nameof(ILO.ILOGroups)))
                .AsEnumerable()
                .SelectMany(parentItem => _unitOfWork
                    .Repository<ILO, long>()
                    .GetAll(_item => _item.ILOSignature == parentItem.ILOSignature, Including: nameof(ILO.ILOGroups)))
                .AsEnumerable()
                .Select(__item =>
                {
                    var treeItem = new TreeItem
                    {
                        Id = __item.Id,
                        Text = __item.Name,
                        Description = __item.Description,
                        IsActive = __item.IsActive,
                        ParentId = __item.ParentId,
                        Code = __item.Code,
                        Signature = __item.OrganizationSignature,
                        IsChildrenLoaded = true,
                        OrganizationId = __item.OrganizationId
                    };

                    treeItem.GroupsIds.AddRange(__item.ILOGroups.Select(x => x.GroupId.ToString()));

                    return treeItem;
                })
                .ToList();

            var response = new TreeItems();

            response.Items.AddRange(data);

            return response;
        }

        public override async Task<TreeItems> GetAllowedParentsOfIloNode(ParentIdCarrier carrier, ServerCallContext context)
        {
            var response = new TreeItems();

            if (carrier?.ParentId == null)
            {
                return response;
            }

            var allowedParents = new List<ILO>();

            await FetchParentsRecursivelyAsync(Convert.ToInt64(carrier.ParentId), allowedParents);

            if (allowedParents.Count == 0)
            {
                return response;
            }

            allowedParents.ForEach(parentNode =>
            {
                response.Items.Add(new TreeItem
                {
                    Id = parentNode.Id,
                    Text = parentNode.Name,
                    Description = parentNode.Description,
                    IsActive = parentNode.IsActive,
                    ParentId = parentNode.ParentId,
                    Code = parentNode.Code,
                    Signature = parentNode.OrganizationSignature,
                    IsChildrenLoaded = true,
                    OrganizationId = parentNode.OrganizationId
                });
            });

            return response;
        }

        private async Task FetchParentsRecursivelyAsync(long? parentId, List<ILO> allParents)
        {
            var parents = await _unitOfWork
                .Repository<ILO, long>()
                .GetAllAsync(ilo => ilo.Id == parentId && !ilo.IsDeleted);

            var parent = parents.FirstOrDefault();

            if (parent != null)
            {
                // Add the current parent
                allParents.Add(parent);

                // If the parent has an id that equals to null, then break the recursion
                if (parent.ParentId == null)
                {
                    return;
                }

                // Fetch siblings
                var siblings = await _unitOfWork
                    .Repository<ILO, long>()
                    .GetAllAsync(ilo => ilo.ParentId == parent.ParentId && ilo.Id != parentId && !ilo.IsDeleted);

                allParents.AddRange(siblings);

                // Recursively fetch the parent of the current parent
                if (parent.ParentId != null)
                {
                    await FetchParentsRecursivelyAsync(parent.ParentId.Value, allParents);
                }
            }
        }
    }
}
