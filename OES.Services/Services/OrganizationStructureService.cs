using Microsoft.EntityFrameworkCore;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.OrganizationStructure.Requests;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class OrganizationStructureService : IOrganizationStructureService
    {
        private readonly ICommonService _commonService;

        public OrganizationStructureService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        // Organization Structure Operations

        public async Task<ApiResponse> GetFlattenedOrganizationStructureAsync(long rootId)
        {
            var repository = _commonService._unitOfWork.Repository<OrganizationStructure, long>();

            var rootNode = await repository.GetObjAsync(p => p.Id == rootId && (p.ParentId == null || p.ParentId == 0));

            if (rootNode == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.NotFound,
                    Resource.OrganizationStructureRootNotFound
                );
            }

            string rootSignature = rootNode.OrganizationStructureSignature;

            var nodes = await repository
                .GetAllAsync(
                    x => x.OrganizationStructureSignature.StartsWith(rootSignature) && x.Id != rootId,
                    Including: nameof(OrganizationStructure.OrganizationNodeLookupItems)
                );

            if (nodes == null || !nodes.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoOrganizationStructureNodesFoundUnderThisRoot,
                    nodes
                );
            }

            var dtoList = nodes.Select(node =>
            {
                bool isFirstLevelChild = node.ParentId == rootId;

                return new GetOrganizationNodeResponseDto(
                    node.Id,
                    node.Name,
                    node.Description,
                    isFirstLevelChild,
                    node.OrganizationNodeLookupItems?
                        .OrderByDescending(x => x.CreationDate)
                        .Select(y => new GetOrganizationNodeLookupItemResponseDto(
                            y.Id,
                            y.Name,
                            y.ParentId,
                            y.OrganizationStructureNodeId))
                        .ToList() ?? []);
            }).ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.OrganizationStructureRetrievedSuccessfully,
                dtoList
            );
        }

        public async Task<ApiResponse> GetAllOrganizationStructureRootsAsync()
        {
            var AllOrganizationStructureRoots = (await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetAllAsync(x => x.ParentId == null || x.ParentId == 0))
                .ToList();

            if (AllOrganizationStructureRoots.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.OrganizationStructureNotFound);
            }

            List<GetOrganizationRootResponseDto> allOrganizationStructureDtos = [];

            foreach (var node in AllOrganizationStructureRoots)
            {
                allOrganizationStructureDtos.Add(new GetOrganizationRootResponseDto(
                    node.Id,
                    node.Name,
                    node.Description
                ));
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.OrganizationStructureRootsRetrievedSuccessfully,
                                allOrganizationStructureDtos);
        }

        public async Task<ApiResponse> GetAllOrganizationStructureAsync(PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetAll(x => x.ParentId == null)
                .AsNoTracking();

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey))
                {
                    if (pagination.SearchInName && pagination.SearchInDescription)
                    {
                        query = query.Where(x => x.Name.Contains(pagination.SearchKey) || x.Description.Contains(pagination.SearchKey));
                    }
                    else if (pagination.SearchInName)
                    {
                        query = query.Where(x => x.Name.Contains(pagination.SearchKey));
                    }
                    else if (pagination.SearchInDescription)
                    {
                        query = query.Where(x => x.Description.Contains(pagination.SearchKey));
                    }
                }

                if (pagination.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                             o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
                }

                query = pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

                var totalItems = await query.CountAsync();

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize).ToListAsync();

                if (data.Count == 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.OrgStructureNodeNotFound);
                }

                var mappedData = data.ConvertAll(x => new GetAllOrganizationResponseDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description
                });

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null,
                                    new CustomTableData<GetAllOrganizationResponseDto>(mappedData, totalItems));
            }
            else
            {
                var data = await (pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate).ToListAsync()
                    : query.OrderBy(x => x.CreationDate).ToListAsync());

                if (data.Count == 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.OrgStructureNodeNotFound);
                }

                var mappedData = data.ConvertAll(x => new GetAllOrganizationResponseDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description
                });

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrgStructureRetrievedSuccessfully,
                                    new CustomTableData<GetAllOrganizationResponseDto>(mappedData, data.Count));
            }
        }

        public async Task<ApiResponse> GetOrganizationStructureByIdAsync(long OrganizationStructureId)
        {
            var OrgStructureData = await _commonService
               ._unitOfWork
               .Repository<OrganizationStructure, long>()
               .GetObjAsync(p => p.Id == OrganizationStructureId);

            if (OrgStructureData == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.OrganizationStructureRootNotFound);
            }

            var mappedData = new AddOrUpdateOrganizationStructureRootRequestDto
            {
                Id = OrgStructureData.Id,
                Name = OrgStructureData.Name,
                Description = OrgStructureData.Description,
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              Resource.OrganizationStructureRetrievedSuccessfully,
                                                              mappedData);
        }

        public async Task<ApiResponse> GetOrganizationRootIdByLookupIdsAsync(List<long> lookupIds)
        {
            if (lookupIds == null || !lookupIds.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.LookupIdListIsEmptyOrNull
                );
            }

            var firstLookupId = lookupIds[0];

            var lookupItem = await _commonService._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .GetObjAsync(x => x.Id == firstLookupId, Including: nameof(OrganizationNodeLookupItem.OrganizationStructureNode));

            if (lookupItem == null || lookupItem.OrganizationStructureNode == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.NotFound,
                    Resource.OrganizationLookupOrNodeNotFound
                );
            }

            string structureSignature = lookupItem.OrganizationStructureNode.OrganizationStructureSignature;

            var existingRoot = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(
                    x => x.ParentId == null && structureSignature.StartsWith(x.OrganizationStructureSignature),
                    Including: nameof(OrganizationStructure.OrganizationNodeLookupItems)
                );

            if (existingRoot == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.OrganizationStructureRootWasNotFound
                );
            }

            var mappedData = new GetOrganizationRootResponseDto(
                existingRoot.Id,
                existingRoot.Name,
                existingRoot.Description
            );

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.OrganizationRootRetrievedSuccessfully,
                mappedData
            );
        }

        // Organization Root Operations

        public async Task<ApiResponse> AddOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto addOrganizationStructureRootDto)
        {
            string currentOrganizationStructureRootSignature = RandomGenerator.GenerateOrganizationStructureRootNodeSignature(addOrganizationStructureRootDto.Name);

            var createdRoot = OrganizationStructure.Create(
                parentId: null,
                name: addOrganizationStructureRootDto.Name,
                description: addOrganizationStructureRootDto.Description,
                organizationStructureSignature: currentOrganizationStructureRootSignature,
                isLeaf: false
            );

            await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .AddAsync(createdRoot);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                var newRootResponseDto = new AddOrUpdateOrganizationStructureRootResponseDto(
                    createdRoot.Id,
                    createdRoot.Name,
                    createdRoot.Description,
                    createdRoot.OrganizationSignature,
                    createdRoot.IsActive
                );

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationStructureRootCreatedSuccessfully,
                                    newRootResponseDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.WrongWhileCreatingOrganizationStructureRoot);
        }

        public async Task<ApiResponse> UpdateOrganizationStructureRootAsync(AddOrUpdateOrganizationStructureRootRequestDto updateOrganizationStructureRootDto)
        {
            var existingRoot = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == updateOrganizationStructureRootDto.Id && x.ParentId == null);

            if (existingRoot == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.OrganizationStructureRootNotFound);
            }

            existingRoot.Update(
                updateOrganizationStructureRootDto.Name,
                updateOrganizationStructureRootDto.Description,
                isLeaf: false
            );

            var rowsAffected = await _commonService._unitOfWork.Complete();

            var updatedRootResponseDto = new AddOrUpdateOrganizationStructureRootResponseDto(
                existingRoot.Id,
                existingRoot.Name,
                existingRoot.Description,
                existingRoot.OrganizationSignature,
                existingRoot.IsActive
            );

            if (rowsAffected > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationStructureRootUpdatedSuccessfully,
                                    updatedRootResponseDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.NoChangesWereMadeBecauseTheRootWasNotUpdated,
                                updatedRootResponseDto);
        }

        public async Task<ApiResponse> DeleteOrganizationStructureRootAsync(long rootId)
        {
            var existingRoot = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == rootId && x.ParentId == null,
                             Including: $"{nameof(OrganizationStructure.OrganizationNodeLookupItems)},{nameof(OrganizationStructure.ChildNodes)}");

            if (existingRoot == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.OrganizationStructureRootNotFound);
            }

            var checkResult = CanDeleteRoot(existingRoot);

            if (!checkResult.CanDeleteRoot)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    checkResult.Message);
            }

            existingRoot.SoftDelete();

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationStructureRootSeletedSuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.SomethingWentWrongWhileDeletingOrganizationStructureRoot);
        }

        // Organization Node Operations

        public async Task<ApiResponse> AddOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto addOrganizationStructureNodeDto)
        {
            var parentNode = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == addOrganizationStructureNodeDto.ParentId, Including: nameof(OrganizationStructure.ChildNodes));

            if (parentNode.ChildNodes.Count > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.CannotAddSiblingNodeToSameParent);
            }

            string currentOrganizationStructureNodeSignature = parentNode.OrganizationStructureSignature;

            var createdNode = OrganizationStructure.Create(
                addOrganizationStructureNodeDto.ParentId,
                addOrganizationStructureNodeDto.Name,
                addOrganizationStructureNodeDto.Description,
                currentOrganizationStructureNodeSignature,
                addOrganizationStructureNodeDto.IsLeaf
            );

            await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .AddAsync(createdNode);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                var newNodeResponseDto = new AddOrUpdateOrganizationStructureNodeResponseDto(
                    createdNode.Id,
                    createdNode.Name,
                    createdNode.Description,
                    createdNode.ParentId,
                    createdNode.IsLeaf,
                    createdNode.OrganizationSignature,
                    createdNode.IsActive
                );

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationStructureNodeCreatedSuccessfully,
                                    newNodeResponseDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.CreateOrganizationStructureNodeFailed);
        }

        public async Task<ApiResponse> UpdateOrganizationStructureNodeAsync(AddOrUpdateOrganizationStructureNodeRequestDto updateOrganizationStructureNodeDto)
        {
            var existingNode = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == updateOrganizationStructureNodeDto.Id, Including: nameof(OrganizationStructure.ChildNodes));

            if (existingNode == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.OrganizationStructureNodeNotFound);
            }

            if (updateOrganizationStructureNodeDto.IsLeaf != existingNode.IsLeaf && existingNode.ChildNodes.Count > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.CannotChangeIsLeafStatusOfOrganizationStructureNodeWithChildNodes);
            }

            existingNode.Update(updateOrganizationStructureNodeDto.Name, updateOrganizationStructureNodeDto.Description, updateOrganizationStructureNodeDto.IsLeaf);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            var updatedNodeResponseDto = new AddOrUpdateOrganizationStructureNodeResponseDto(
                    existingNode.Id,
                    existingNode.Name,
                    existingNode.Description,
                    existingNode.ParentId,
                    existingNode.IsLeaf,
                    existingNode.OrganizationSignature,
                    existingNode.IsActive
                );

            if (rowsAffected > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationStructureNodeUpdatedSuccessfully,
                                    updatedNodeResponseDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.OrganizationStructureNodeNotUpdated,
                                updatedNodeResponseDto);
        }

        public async Task<ApiResponse> DeleteOrganizationStructureNodeAsync(long nodeId)
        {
            var existingNode = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == nodeId, Including: $"{nameof(OrganizationStructure.OrganizationNodeLookupItems)},{nameof(OrganizationStructure.ChildNodes)}");

            if (existingNode == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.OrgStructureNodeNotFound);
            }

            var checkResult = CanDeleteNode(existingNode);

            if (!checkResult.CanDeleteNode)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    checkResult.Message);
            }

            existingNode.SoftDelete();

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationNodeDeletedSuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.DeleteOrganizationNodeFailed);
        }

        // Organization Node Lookup Items Operations

        public async Task<ApiResponse> GetNodeLookupItemsAsync(long nodeId)
        {
            var nodeLookupItemsDtos = await _commonService
                ._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .GetAll(x => x.OrganizationStructureNodeId == nodeId)
                .OrderByDescending(x => x.CreationDate)
                .Select(x => new GetOrganizationNodeLookupItemResponseDto(x.Id, x.Name, x.ParentId, x.OrganizationStructureNodeId))
                .ToListAsync();

            if (nodeLookupItemsDtos.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.NoOrganizationStructureNodeLookupItemsFound);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.OrganizationNodeLookupItemsRetrievedSuccessfully,
                                nodeLookupItemsDtos);
        }

        public async Task<ApiResponse> GetAllNodeLookupItemsAsync()
        {
            var allNodeLookupItemsDtos = await _commonService
                ._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .GetAllAsync(Including: nameof(OrganizationNodeLookupItem.OrganizationStructureNode));

            if (!allNodeLookupItemsDtos.Any())
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.NoOrganizationStructureNodeLookupItemsFound);
            }

            var responseDtos = allNodeLookupItemsDtos.Select(x => new GetOrganizationStructureForImportResponseDto(
                x.Id,
                x.Name,
                x.OrganizationStructureNode?.Name,
                x.ParentId,
                x.OrganizationStructureNodeId,
                x.IsRoot
            )).ToList();

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.AllOrganizationNodeLookupItemsRetrievedSuccessfully,
                                responseDtos);
        }

        public async Task<ApiResponse> GetChildLookupItemsByParentLookupItemIdAsync(long parentLookupItemId)
        {
            var childLookupItemsDtos = await _commonService
                ._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .GetAll(x => x.ParentId == parentLookupItemId, Including: nameof(OrganizationNodeLookupItem.OrganizationStructureNode))
                .OrderByDescending(x => x.CreationDate)
                .Select(x => new GetOrganizationNodeLookupItemForViewResponseDto(x.Id, x.Name, x.OrganizationStructureNode.Id, x.OrganizationStructureNode.Name))
                .ToListAsync();

            if (childLookupItemsDtos.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.NoChildLookupItemsFound);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.ChildLookupItemsRetrievedSuccessfully,
                                childLookupItemsDtos);
        }

        public async Task<ApiResponse> AddNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto addOrganizationNodeLookupItemRequestDto)
        {
            var targetNode = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == addOrganizationNodeLookupItemRequestDto.OrganizationStructureNodeId, nameof(OrganizationStructure.ParentOrganizationStructureNode));

            if (targetNode == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.OrganizationStructureNodeNotFound);
            }

            if (targetNode.ParentId == null || targetNode.ParentId == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.CannotAddLookupItemToRootNode);
            }

            var isCurrentLookupItemRoot = targetNode.ParentOrganizationStructureNode.ParentId == null || targetNode.ParentOrganizationStructureNode.ParentId == 0;

            var createdItem = OrganizationNodeLookupItem.Create(
                addOrganizationNodeLookupItemRequestDto.Name,
                isCurrentLookupItemRoot,
                addOrganizationNodeLookupItemRequestDto.ParentLookupItemId,
                addOrganizationNodeLookupItemRequestDto.OrganizationStructureNodeId
            );

            await _commonService
                ._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .AddAsync(createdItem);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                var newLookupItemDto = new AddOrUpdateOrganizationNodeLookupItemResponseDto(
                    createdItem.Id,
                    createdItem.Name,
                    createdItem.ParentId,
                    createdItem.OrganizationStructureNodeId
                );

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationNodeLookupItemCreatedSuccessfully,
                                    newLookupItemDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.CreateOrganizationNodeLookupItemFailed);
        }

        public async Task<ApiResponse> UpdateNodeLookupItemAsync(AddOrUpdateOrganizationNodeLookupItemRequestDto updateOrganizationNodeLookupItemRequestDto)
        {
            var existingItem = await _commonService
                ._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .GetObjAsync(x => x.Id == updateOrganizationNodeLookupItemRequestDto.Id);

            if (existingItem == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.OrganizationNodeLookupItemNotFound);
            }

            existingItem.Update(updateOrganizationNodeLookupItemRequestDto.Name, updateOrganizationNodeLookupItemRequestDto.ParentLookupItemId);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            var updatedLookupItemDto = new AddOrUpdateOrganizationNodeLookupItemResponseDto(
                   existingItem.Id,
                   existingItem.Name,
                   existingItem.ParentId,
                   existingItem.OrganizationStructureNodeId
               );

            if (rowsAffected > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationNodeLookupItemUpdatedSuccessfully,
                                    updatedLookupItemDto);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.OrganizationNodeLookupItemNotUpdated,
                                updatedLookupItemDto);
        }

        public async Task<ApiResponse> DeleteNodeLookupItemAsync(long nodeLookupItemId)
        {
            var existingItem = await _commonService
                ._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .GetObjAsync(x => x.Id == nodeLookupItemId, Including: nameof(OrganizationNodeLookupItem.ChildLookupItems));

            var checkResult = CanDeleteNodeLookupItem(existingItem);

            if (!checkResult.CanDeleteNodeLookupItem)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    checkResult.Message);
            }

            existingItem.SoftDelete();

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.OrganizationNodeLookupItemDeletedSuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.DeleteOrganizationNodeLookupItemFailed);
        }


        #region Helper Methods

        private static (bool CanDeleteNode, string Message) CanDeleteNode(OrganizationStructure organizationStructureNode)
        {
            if (organizationStructureNode == null)
            {
                return (false, Resource.OrganizationStructureNodeNotFound);
            }

            if (organizationStructureNode.ChildNodes.Count > 0)
            {
                return (false, Resource.CannotDeleteOrganizationStructureNodeWithChildNodes);
            }

            if (organizationStructureNode.OrganizationNodeLookupItems.Count > 0)
            {
                return (false, Resource.CannotDeleteOrganizationStructureNodeWithLookupItems);
            }

            return (true, string.Empty);
        }

        private static (bool CanDeleteNodeLookupItem, string Message) CanDeleteNodeLookupItem(OrganizationNodeLookupItem organizationNodeLookupItem)
        {
            if (organizationNodeLookupItem == null)
            {
                return (false, Resource.OrganizationNodeLookupItemNotFound);
            }

            if (organizationNodeLookupItem.ChildLookupItems.Count > 0)
            {
                return (false, Resource.CannotDeleteOrganizationNodeLookupItemWithChildItems);
            }

            return (true, string.Empty);
        }

        private static (bool CanDeleteRoot, string Message) CanDeleteRoot(OrganizationStructure organizationStructureRoot)
        {
            if (organizationStructureRoot == null)
            {
                return (false, Resource.OrganizationStructureRootNotFound);
            }

            if (organizationStructureRoot.ChildNodes.Count > 0)
            {
                return (false, Resource.CannotDeleteOrganizationStructureRootWithChildNodes);
            }

            if (organizationStructureRoot.OrganizationNodeLookupItems.Count > 0)
            {
                return (false, Resource.CannotDeleteOrganizationStructureRootWithLookupItems);
            }

            return (true, string.Empty);
        }

        #endregion Helper Methods
    }
}