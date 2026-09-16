using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using SharedHelper.RolesNames;
using System.Collections.Concurrent;
using System.Net;

namespace OES.Services.Services
{
    public class ILOService : IILOService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;
        private readonly IILOValidatorService _iLOValidatorService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly INotificationService _notificationService;
        private readonly IAutoPermissionAssignmentService _autoPermissionAssignmentService;

        public ILOService(ICommonService commonService,
                          IMapper mapper,
                          IILOValidatorService iLOValidatorService,
                          FilterParamsValues filterParamsValues,
                          INotificationService notificationService,
                          IAutoPermissionAssignmentService autoPermissionAssignmentService)
        {
            _commonService = commonService;
            _mapper = mapper;
            _iLOValidatorService = iLOValidatorService;
            _filterParamsValues = filterParamsValues;
            _notificationService = notificationService;
            _autoPermissionAssignmentService = autoPermissionAssignmentService;
        }

        public async Task<IApiResponse> GetByIdAsync(long id)
        {
            var ilo = await _commonService
                ._unitOfWork
                .Repository<ILO, long>()
                .GetByIdAsync(id);

            if (ilo != null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.FetchSuccess,
                                    new TreeItemResponseDto
                                    {
                                        Id = ilo.Id,
                                        Text = ilo.Name,
                                        Description = ilo.Description,
                                        ParentId = ilo.ParentId,
                                        Signature = ilo.OrganizationSignature,
                                        Code = ilo.Code,
                                        OrganizationId = ilo.OrganizationId,
                                    });
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.IloNotFound);
        }

        public async Task<IApiResponse> GetPagedILOS(PaginationSearchModel pagination)
        {
            var query = _commonService._unitOfWork
                                      .Repository<ILO, long>()
                                      .GetAll(x => x.ParentId == null || x.ParentId == 0)
                                      .AsNoTracking();

            var isAdmin = _filterParamsValues
                .SsoUserRoles
                .Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            if (!isAdmin)
            {
                var userGroupIds = _filterParamsValues
                    .OesUserGroupsAndRoles
                    .Select(g => g.GroupId)
                    .ToHashSet();

                query = query.Where(item =>
                    item.ILOGroups.Any(g => !g.IsDeleted) &&
                    item.ILOGroups.Any(g => userGroupIds.Contains(g.GroupId) && !g.IsDeleted)
                );
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

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
                        query = query.Where(x => x.Code.Contains(pagination.SearchKey));
                    }
                    else if (pagination.SearchInDescription)
                    {
                        query = query.Where(x => x.Name.Contains(pagination.SearchKey));
                    }
                }

                if (pagination.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                             o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
                }

                var totalItems = await query.CountAsync();

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize)
                                      .Take(pagination.PageSize)
                                      .ToListAsync();

                var mappedData = _mapper.Map<List<ILODto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaginationIsOn,
                    new CustomTableData<ILODto>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await query.ToListAsync();

                var mappedData = _mapper.Map<List<ILODto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaginationIsOff,
                    new CustomTableData<ILODto>(mappedData, data.Count)
                );
            }
        }

        public async Task<ApiResponse> AddNewRoot(ILONewRootDto dto)
        {
            var Validator = await _iLOValidatorService.ValidateNewRoot(dto);

            if (Validator.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return Validator;
            }

            var existingIlo = await _commonService._unitOfWork
                .Repository<ILO, long>()
                .GetObjAsync(x => x.Name.ToLower().Trim() == dto.Name.ToLower().Trim() || x.Code.ToLower().Trim() == dto.Code.ToLower().Trim());

            if (existingIlo != null)
            {
                if (existingIlo.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.Conflict,
                        Resource.RootIloNameAlreadyExists
                    );
                }

                if (existingIlo.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase))
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.Conflict,
                        Resource.RootIloCodeAlreadyExists
                    );
                }
            }

            ILO NewRoot = new()
            {
                Name = dto.Name?.Trim(),
                IsActive = true,
                Description = dto.Description,
                Code = dto.Code?.Trim(),
                ParentId = null,
                OrganizationSignature = _filterParamsValues.Signature,
                ILOSignature = RandomGenerator.GenerateIloSignatureCode(dto.Name)
            };

            await _commonService._unitOfWork.Repository<ILO, long>().AddAsync(NewRoot);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var request = new AutoPermissionAssignmentRequest
                {
                    EntityId = NewRoot.Id,
                    EntityName = NewRoot.Name,
                    EntityGroupType = typeof(ILOGroup),
                    ResourceType = ResourceType.Ilo,
                    AdditionalGroupIds = dto.GroupsId?.Any() == true
                    ? dto.GroupsId : null
                };

                var groupRepo = _commonService._unitOfWork.Repository<ILOGroup, long>();

                if (dto.GroupsId?.Any() == true)
                {
                    foreach (var gid in dto.GroupsId.Distinct())
                    {
                        await groupRepo.AddAsync(new ILOGroup
                        {
                            ILOId = NewRoot.Id,
                            GroupId = gid,
                            CreationUser = _filterParamsValues.UserEmail ?? nameof(System),
                            IsDeleted = false
                        });
                    }
                }

                await _commonService._unitOfWork.Complete();

                var isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin);

                if (!isSuperAdmin)
                {
                    await _autoPermissionAssignmentService.AssignDefaultPermissionsForNewEntityAsync(request);
                }

                CreateNewNotificationDto newNotification = new()
                {
                    Entity = NotificationEntity.ILO,
                    Operation = NotificationOperation.Added,
                    Status = NotificationStatus.Success,
                    Type = NotificationTypeStatus.Success,
                    AffectedRows = 1,
                    ParameterName = dto.Name,
                    From = Resource.Notification_From_System
                };

                if (dto.GroupsId?.Count > 0)
                {
                    await _notificationService.SendNotificationForNewItemBank(dto.GroupsId, newNotification);
                }

                await _commonService._unitOfWork.Complete();

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.RootILOAddedSuccessfully
                );
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.FailToAddILO
                );
            }
        }

        public async Task<ApiResponse> EditILORoot(IloEditDTO dto)
        {
            var Validator = await _iLOValidatorService.ValidateEditRoot(dto);

            if (Validator.CustomCodeStatus != CustomCodeStatus.Success)
                return Validator;

            var repo = _commonService._unitOfWork.Repository<ILO, long>();

            var groupRepo = _commonService._unitOfWork.Repository<ILOGroup, long>();

            var ilo = await repo.GetObjAsync(
                x => x.Id == dto.Id,
                Including: "ILOGroups.OESGroup"
            );

            if (ilo == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.IloNotFound
                );
            }

            var isNameChanged = !string.Equals(
                ilo.Name?.Trim(),
                dto.Name?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

            var isCodeChanged = !string.Equals(
                ilo.Code?.Trim(),
                dto.Code?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );

            if (isNameChanged || isCodeChanged)
            {
                var duplicate = await repo.GetObjAsync(
                    x => x.Id != dto.Id &&
                    (
                        (isNameChanged && x.Name.ToLower().Trim() == dto.Name.ToLower().Trim()) ||
                        (isCodeChanged && x.Code.ToLower().Trim() == dto.Code.ToLower().Trim())
                    )
                );

                if (duplicate != null)
                {
                    var message =
                        isNameChanged &&
                        duplicate.Name.ToLower().Trim() == dto.Name.ToLower().Trim()
                            ? Resource.RootIloNameAlreadyExists
                            : Resource.RootIloCodeAlreadyExists;

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.Conflict,
                        message
                    );
                }
            }

            var ownerGroup = ilo.ILOGroups.FirstOrDefault(g => !g.IsDeleted && g.OESGroup.AutoCreatedForUser);

            var ownerGroupId = ownerGroup?.GroupId ?? Guid.Empty;

            var newAssignedIds = dto.GroupsId?.ToHashSet() ?? new HashSet<Guid>();

            var toRemove = ilo.ILOGroups
                .Where(g =>
                    g.GroupId != ownerGroupId &&
                    !newAssignedIds.Contains(g.GroupId) &&
                    !g.IsDeleted
                )
                .ToList();

            if (toRemove.Count > 0)
                groupRepo.DeleteRange(toRemove);

            var existingIds = ilo.ILOGroups
                .Where(g => !g.IsDeleted)
                .Select(g => g.GroupId)
                .ToHashSet();

            var toAdd = newAssignedIds
                .Where(id => id != ownerGroupId && !existingIds.Contains(id))
                .ToList();

            foreach (var gid in toAdd)
            {
                await groupRepo.AddAsync(new ILOGroup
                {
                    ILOId = ilo.Id,
                    GroupId = gid,
                    CreationUser = _filterParamsValues.UserEmail ?? "System"
                });
            }

            await _commonService._unitOfWork.Complete();
            ilo.Name = dto.Name?.Trim();
            ilo.Description = dto.Description;
            ilo.Code = dto.Code?.Trim();

            repo.UpdateWithTracking(ilo);

            var result = await _commonService._unitOfWork.Complete();

            return result > 0
                ? _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ILOUpdatedSuccessfully
                )
                : _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.NotModified,
                    Resource.FailToUpdateThisILO
                );
        }

        public async Task<ApiResponse> GetILORoot(long Id)
        {

            if (await _commonService._unitOfWork.Repository<ILO, long>().IsExistAsync(e => e.Id == Id))
            {
                var Ilo = await _commonService
                ._unitOfWork
                .Repository<ILO, long>()
                .GetObjAsync(
                    e => e.Id == Id,
                    Including: "ILOGroups.OESGroup"
                );

                if (Ilo == null)
                {
                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.IloNotFound);
                }

                ILODto Dto = new()
                {
                    Id = Ilo.Id,
                    Name = Ilo.Name,
                    Description = Ilo.Description,
                    Code = Ilo.Code,
                    GroupsIds = [.. Ilo.ILOGroups.Where(g => !g.IsDeleted).Select(g => g.GroupId)]
                };


                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, Dto);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, Resource.AnErrorOccurredWhileFetchingTheIlo);
            }

        }

        public async Task<List<TreeItemResponseDto>> GetByParentIdAndSignatureAsync(TreeItemRequestDto iloRequestDto)
        {
            var rootIlos = (await _commonService._unitOfWork.Repository<ILO, long>()
                .GetAllAsync(ilo => ilo.ParentId == iloRequestDto.ParentId &&
                             (string.IsNullOrWhiteSpace(iloRequestDto.Signature) ||
                             ilo.OrganizationSignature == iloRequestDto.Signature)))
                .ToList();

            var ilosDtos = new ConcurrentBag<TreeItemResponseDto>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 3
            };

            await Parallel.ForEachAsync(rootIlos, parallelOptions, async (ilo, token) =>
            {
                var dto = new TreeItemResponseDto
                {
                    Id = ilo.Id,
                    Text = ilo.Name,
                    Description = ilo.Description,
                    ParentId = ilo.ParentId,
                    Code = ilo.Code,
                    IsActive = ilo.IsActive,
                    Signature = ilo.OrganizationSignature,
                    OrganizationId = ilo.OrganizationId
                };

                ilosDtos.Add(dto);
            });

            return ilosDtos.ToList();
        }

        public async Task<ApiResponse> InsertNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto)
        {
            var repo = _commonService._unitOfWork.Repository<ILO, long>();

            var similarNode = await repo.GetObjAsync(
                x => x.ParentId == iloRequestDto.ParentId && (x.Name == iloRequestDto.Name || x.Code == iloRequestDto.Code)
            );

            if (similarNode != null)
            {
                if (similarNode.Code == iloRequestDto.Code)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Conflict,
                        HttpStatusCode.Conflict,
                        Resource.ILONodeCodeAlreadyExists
                    );
                }

                if (similarNode.Name == iloRequestDto.Name)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Conflict,
                        HttpStatusCode.Conflict,
                        Resource.ILONodeNameAlreadyExists
                    );
                }
            }

            var parentNode = await repo.GetObjAsync(x => x.Id == iloRequestDto.ParentId, "ILOGroups.OESGroup");

            if (parentNode == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.ParentNotFound);

            var mappedIlo = _mapper.Map<ILO>(iloRequestDto);

            mappedIlo.ILOSignature = parentNode.ILOSignature;
            mappedIlo.OrganizationSignature = parentNode.OrganizationSignature ?? _filterParamsValues.Signature;

            await repo.AddAsync(mappedIlo);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                var groupRepo = _commonService._unitOfWork.Repository<ILOGroup, long>();

                var parentOwnerGroupId = parentNode
                    .ILOGroups
                    .FirstOrDefault(g => g.OESGroup.AutoCreatedForUser && !g.IsDeleted)
                    ?.GroupId;

                var parentAssignedGroupIds = parentNode.ILOGroups
                    .Where(g => !g.OESGroup.AutoCreatedForUser && !g.IsDeleted)
                    .Select(g => g.GroupId)
                    .ToList();

                List<Guid> uiGroupIds = iloRequestDto.GroupsIds ?? [];

                var finalVisibleGroups = parentAssignedGroupIds
                    .Union(uiGroupIds)
                    .Distinct()
                    .ToList();

                List<Guid> finalGroups = [];

                if (parentOwnerGroupId != null)
                    finalGroups.Add(parentOwnerGroupId.Value);

                finalGroups.AddRange(finalVisibleGroups);

                foreach (var gid in finalGroups.Distinct())
                {
                    await groupRepo.AddAsync(new ILOGroup
                    {
                        ILOId = mappedIlo.Id,
                        GroupId = gid,
                        CreationUser = _filterParamsValues.UserEmail
                    });
                }

                await _commonService._unitOfWork.Complete();

                var permissionRequest = new AutoPermissionAssignmentRequest
                {
                    EntityId = mappedIlo.Id,
                    EntityName = mappedIlo.Name,
                    EntityGroupType = typeof(ILOGroup),
                    ResourceType = ResourceType.Ilo,
                    AdditionalGroupIds = finalGroups
                };

                var isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin);

                if (!isSuperAdmin)
                {
                    await _autoPermissionAssignmentService.AssignDefaultPermissionsForNewEntityAsync(permissionRequest);
                }

                var newNotification = new CreateNewNotificationDto
                {
                    Entity = NotificationEntity.ILO,
                    Operation = NotificationOperation.Added,
                    Status = NotificationStatus.Success,
                    Type = NotificationTypeStatus.Success,
                    AffectedRows = 1,
                    ParameterName = iloRequestDto.Name,
                    From = Resource.Notification_From_System
                };

                if (iloRequestDto.GroupsIds?.Count > 0)
                {
                    await _notificationService.SendNotificationForNewItemBank(iloRequestDto.GroupsIds, newNotification);
                }

                var mappedTreeItemDto = _mapper.Map<TreeItemResponseDto>(mappedIlo);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ILONodeHasJustBeenInsertedSuccessfully,
                    mappedTreeItemDto
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.BadRequest,
                Resource.FailToSaveILONode
            );
        }

        public async Task<IApiResponse> EditNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto)
        {
            var repo = _commonService._unitOfWork.Repository<ILO, long>();

            var similarNode = await repo.GetObjAsync(
                x => x.ParentId == iloRequestDto.ParentId && x.Id != iloRequestDto.Id && (x.Name == iloRequestDto.Name || x.Code == iloRequestDto.Code)
            );

            if (similarNode != null)
            {
                if (similarNode.Code == iloRequestDto.Code)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Conflict,
                        HttpStatusCode.Conflict,
                        Resource.ILONodeCodeAlreadyExists
                    );
                }

                if (similarNode.Name == iloRequestDto.Name)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Conflict,
                        HttpStatusCode.Conflict,
                        Resource.ILONodeNameAlreadyExists
                    );
                }
            }

            var ilo = await repo.GetObjAsync(
                x => x.Id == iloRequestDto.Id,
                Including: "ILOGroups.OESGroup"
            );

            if (ilo == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.IloNotFound
                );
            }

            ilo.Name = iloRequestDto.Name?.Trim();
            ilo.Description = iloRequestDto.Description;
            ilo.Code = iloRequestDto.Code?.Trim();
            ilo.IsActive = iloRequestDto.IsActive;
            ilo.ParentId = iloRequestDto.ParentId;

            var parentNode = await repo.GetObjAsync(x => x.Id == iloRequestDto.ParentId);

            if (parentNode != null)
                ilo.ILOSignature = parentNode.ILOSignature;

            var existingGroups = ilo.ILOGroups
                .Where(g => !g.IsDeleted)
                .ToList();

            var ownerGroup = existingGroups.FirstOrDefault(g => g.OESGroup != null && g.OESGroup.AutoCreatedForUser);

            var requestedGroupIds = iloRequestDto.GroupsIds ?? [];

            var groupRepo = _commonService._unitOfWork.Repository<ILOGroup, long>();

            foreach (var group in existingGroups)
            {
                bool isOwner = ownerGroup != null && group.GroupId == ownerGroup.GroupId;

                bool isStillSelected = requestedGroupIds.Contains(group.GroupId);

                if (!isOwner && !isStillSelected)
                {
                    ilo.ILOGroups.Remove(group);
                }
            }

            foreach (var groupId in requestedGroupIds)
            {
                bool alreadyExists = existingGroups.Any(g => g.GroupId == groupId);

                if (!alreadyExists)
                {
                    await groupRepo.AddAsync(new ILOGroup
                    {
                        ILOId = ilo.Id,
                        GroupId = groupId
                    });
                }
            }

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                var mappedTreeItemDto = _mapper.Map<TreeItemResponseDto>(ilo);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ILOHasBeenUpdatedSuccessfully,
                    mappedTreeItemDto);
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.BadRequest,
                Resource.FailToUpdateThisILO);
        }

        public async Task<ApiResponse> GetParentsAsync(long? parentId)
        {
            if (parentId == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.InvalidParameter, HttpStatusCode.BadRequest, Resource.ParentIdCannotBeNull);
            }

            var allParents = new List<ILO>();

            await FetchParentsRecursively(parentId.Value, allParents);

            if (allParents.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.ParentNotFound);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, allParents);
        }

        public async Task<IApiResponse> SoftDeleteRootAsync(long id)
        {
            var unitOfWork = _commonService._unitOfWork.Repository<ILO, long>();

            var includes = $"{nameof(ILO.ILOGroups)}";

            var iLO = await unitOfWork.GetObjAsync(e => e.Id == id, includes);

            if (iLO != null)
            {
                // Validate if the ILO can be deleted
                bool canDelete = await CanDeleteILOAsync(id);

                if (!canDelete)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Forbidden,
                        HttpStatusCode.Forbidden,
                        Resource.ILOCannotBeDeletedDueToValidationRules
                    );
                }

                unitOfWork.SoftDelete(iLO);


                foreach (var group in iLO.ILOGroups)
                {
                    _commonService
                      ._unitOfWork
                      .Repository<ILOGroup, Guid>()
                      .SoftDelete(group);
                }

                // Fetch children ilo NOTE search by org-signature
                var children = await unitOfWork
                    .GetAll()
                    .Where(o => o.ParentId == iLO.Id && o.IsDeleted == false && o.OrganizationSignature == iLO.OrganizationSignature)
                    .ToListAsync();

                // Update children ilo to point to the parent of the organization being soft deleted
                foreach (var child in children)
                {
                    child.ParentId = iLO.ParentId;
                }

                // Commit the transaction
                await _commonService._unitOfWork.Complete();

                //return successfully
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ILOhasbeendeletedsuccessfully, iLO
                );
            }
            else
            {
                // Return a not found response
                return _commonService._apiResponse.GetApiResponse(
                   CustomCodeStatus.NotFound,
                   HttpStatusCode.NotFound,
                   string.Format(Resource.IloWithIdNotFound, id)
                );
            }
        }

        public async Task<IApiResponse> SoftDeleteRootWithChildrenAsync(long id)
        {
            var unitOfWork = _commonService._unitOfWork.Repository<ILO, long>();

            var ILO = await unitOfWork.GetObjAsync(e => e.Id == id);

            if (ILO != null)
            {
                // Validate if the ilo can be deleted
                bool canDelete = await CanDeleteILOAsync(id);

                if (!canDelete)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Forbidden,
                        HttpStatusCode.Forbidden,
                        Resource.ILOCannotBeDeletedDueToValidationRules
                    );
                }

                //Fetch all nested children
                var ILOWithSameORG = await unitOfWork
                    .GetAll()
                    .Where(o => o.OrganizationSignature == ILO.OrganizationSignature)
                    .ToListAsync();

                var nestedChildren = await GetAllNestedChildrenAsync(ILO.Id, ILOWithSameORG);

                foreach (var item in nestedChildren)
                {
                    unitOfWork.SoftDelete(item);
                }

                unitOfWork.SoftDelete(ILO);

                await _commonService._unitOfWork.Complete();

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ILOhasbeendeletedsuccessfully,
                    ILO
                );
            }
            else
            {
                // Return a not found response
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    $"ILO with ID {id} not found."
                );
            }
        }

        public async Task<IApiResponse> CanSoftDeleteIloAsync(long id)
        {
            if (await CanDeleteILOAsync(id))
            {
                return _commonService
                      ._apiResponse
                      .GetApiResponse(CustomCodeStatus.Success,
                                      HttpStatusCode.Accepted,
                                      Resource.ThisILOIsEligibleForDeletion);
            }

            return _commonService
                  ._apiResponse
                  .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                  HttpStatusCode.NotAcceptable,
                                  Resource.ThisILORootHasAQuestionAttachedToIt);
        }

        public async Task<ApiResponse> ExecuteSoftDeleteForNodeAsync(long id)
        {
            var linkedQuestionsCodes = await GetIloLinkedQuestionCodesRecursivelyAsync(id);

            if (linkedQuestionsCodes.Count > 0)
            {
                var codesString = string.Join(", ", linkedQuestionsCodes.Distinct());

                var errorMessage = $"{Resource.CannotDeleteILO}. {Resource.LinkedQuestionsCodes} {codesString}";

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.Forbidden,
                                    errorMessage);
            }

            var repository = _commonService._unitOfWork.Repository<ILO, long>();

            var iloList = new List<ILO>
            {
                await repository.GetByIdAsync(id)
            };

            iloList.AddRange(await GetNestedNodesOfIloAsync(id));

            iloList.ForEach(repository.SoftDelete);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.ILOhasbeendeletedsuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.Conflict,
                                Resource.FailedToDeleteILO);
        }

        public async Task<ApiResponse> TransferChildrenForNodeAsync(long id)
        {
            var linkedQuestionsCodes = await GetIloLinkedQuestionCodesRecursivelyAsync(id);

            if (linkedQuestionsCodes.Count > 0)
            {
                var codesString = string.Join(", ", linkedQuestionsCodes.Distinct());

                var errorMessage = $"{Resource.CannotDeleteILO}. {Resource.LinkedQuestionsCodes} {codesString}";

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.Forbidden,
                                    errorMessage);
            }

            var repository = _commonService._unitOfWork.Repository<ILO, long>();

            var iloNode = await repository.GetByIdAsync(id);

            repository.SoftDelete(iloNode);

            var directChildrenList = (await repository.GetAllAsync(item => item.ParentId == id)).ToList();

            directChildrenList.ForEach(itemBank => itemBank.ParentId = iloNode.ParentId);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.ILOhasbeendeletedanditsdirectnodeshavebeentransferredtoahigherparentnodesuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.Conflict,
                                Resource.SomethingwentwrongregardingcurrentILOdeletionandtransferringnodes);
        }

        public async Task<IApiResponse> GetRootNodeILO()
        {
            var rootNode = await _commonService
               ._unitOfWork
               .Repository<ILO, long>()
               .GetAllAsync(x => x.ParentId == null);

            if (rootNode == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.IloNotFound);
            }

            var mappedRootNode = _mapper.Map<List<RootIloDto>>(rootNode);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                mappedRootNode);
        }

        public async Task<ApiResponse> GetIloGroupsAsync(long iloId)
        {
            var iloGroups = await _commonService
                ._unitOfWork
                .Repository<ILOGroup, long>()
                .GetAllAsync(x =>
                    x.ILOId == iloId &&
                    !x.IsDeleted &&
                    x.OESGroup != null &&
                    !x.OESGroup.IsTemplate,
                    Including: "OESGroup"
                );

            var iloGroupsDto = new IloGroupsDto();

            iloGroups.ToList().ForEach(ig => iloGroupsDto.GroupsIds.Add(ig.GroupId));

            var owner = iloGroups.FirstOrDefault(g => g.OESGroup != null && g.OESGroup.AutoCreatedForUser);

            iloGroupsDto.OwnerGroupId = owner?.GroupId;

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              iloGroupsDto);
        }

        public async Task<ApiResponse> GetUserIloGroupAsync()
        {
            var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";

            var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

            var groups = await groupRepo.GetAllAsync(g =>
                !g.IsDeleted &&
                !g.IsPredefined &&
                !g.IsTemplate &&
                !g.AutoCreatedForUser &&
                g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Ilo) &&
                (
                    g.CreationUser.ToLower() == currentUser ||
                    g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
                ),
                Including: nameof(OESGroup.GroupResources)
            );

            var groupDtos = groups
                .Select(g => new GetOESGroupDto
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .DistinctBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                groupDtos
            );
        }

        #region Helper Methods
        private static async Task<List<ILO>> GetAllNestedChildrenAsync(long parentId, List<ILO> directChildren)
        {
            directChildren = [.. directChildren.Where(o => o.ParentId == parentId)];

            var nestedChildren = new List<ILO>();

            nestedChildren.AddRange(directChildren);

            foreach (var child in directChildren)
            {
                var childNestedChildren = await GetAllNestedChildrenAsync(child.Id, directChildren);

                nestedChildren.AddRange(childNestedChildren);
            }

            return nestedChildren;
        }

        private async Task<List<string>> GetIloLinkedQuestionCodesRecursivelyAsync(long iloId)
        {
            var nestedChildren = await GetNestedNodesOfIloAsync(iloId);

            var allIloIds = nestedChildren.ConvertAll(i => i.Id);

            allIloIds.Add(iloId);

            var linkedQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(q => q.IloId.HasValue && allIloIds.Contains(q.IloId.Value));

            return [.. linkedQuestions.Select(q => q.Code)];
        }

        private async Task<List<ILO>> GetNestedNodesOfIloAsync(long parentId)
        {
            var repository = _commonService._unitOfWork.Repository<ILO, long>();

            var nestedChildren = new List<ILO>();

            var directChildren = await repository.GetAllAsync(item => item.ParentId == parentId);

            foreach (var child in directChildren)
            {
                nestedChildren.Add(child);

                var _nestedChildren = await GetNestedNodesOfIloAsync(child.Id);

                nestedChildren.AddRange(_nestedChildren);
            }

            return nestedChildren;
        }

        private async Task FetchParentsRecursively(long parentId, List<ILO> allParents)
        {
            var parents = await _commonService
                ._unitOfWork
                .Repository<ILO, long>()
                .GetAllAsync(ilo => ilo.Id == parentId);

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
                var siblings = await _commonService
                    ._unitOfWork
                    .Repository<ILO, long>()
                    .GetAllAsync(ilo => ilo.ParentId == parent.ParentId && ilo.Id != parentId);

                allParents.AddRange(siblings);

                // Recursively fetch the parent of the current parent
                if (parent.ParentId != null)
                {
                    await FetchParentsRecursively(parent.ParentId.Value, allParents);
                }
            }
        }

        private async Task<bool> CanDeleteILOAsync(long iloId)
        {
            var hasQuestions = await _commonService._unitOfWork.Repository<QuestionMetadata, long>().IsExistAsync(e => e.IloId == iloId);

            return !hasQuestions;
        }
        #endregion
    }
}
