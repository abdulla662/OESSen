using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.AutoPermissionAssignment.Response;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using SharedHelper.General;
using SharedHelper.RolesNames;

namespace OES.Services.Services
{
    public class AutoPermissionAssignmentService : IAutoPermissionAssignmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly FilterParamsValues _filter;
        private readonly Dictionary<Type, Func<object, Task>> _addEntityGroupMap;

        public AutoPermissionAssignmentService(
            IUnitOfWork unitOfWork,
            FilterParamsValues filter
        )
        {
            _unitOfWork = unitOfWork;
            _filter = filter;
            _addEntityGroupMap = new Dictionary<Type, Func<object, Task>>
            {
                [typeof(ItemBankGroups)] = async (entity) =>
                {
                    var repo = _unitOfWork.Repository<ItemBankGroups, long>();
                    await repo.AddAsync((ItemBankGroups)entity);
                },

                [typeof(QuestionGroups)] = async (entity) =>
                {
                    var repo = _unitOfWork.Repository<QuestionGroups, long>();
                    await repo.AddAsync((QuestionGroups)entity);
                },

                [typeof(ILOGroup)] = async (entity) =>
                {
                    var repo = _unitOfWork.Repository<ILOGroup, long>();
                    await repo.AddAsync((ILOGroup)entity);
                },

                [typeof(PaperGroups)] = async (entity) =>
                {
                    var repo = _unitOfWork.Repository<PaperGroups, long>();
                    await repo.AddAsync((PaperGroups)entity);
                },

                [typeof(ScheduleGroups)] = async (entity) =>
                {
                    var repo = _unitOfWork.Repository<ScheduleGroups, long>();
                    await repo.AddAsync((ScheduleGroups)entity);
                },

                [typeof(BlockGroups)] = async (entity) =>
                {
                    var repo = _unitOfWork.Repository<BlockGroups, long>();
                    await repo.AddAsync((BlockGroups)entity);
                },
            };
        }

        public async Task<AutoPermissionAssignmentResponse> AssignDefaultPermissionsForNewEntityAsync(AutoPermissionAssignmentRequest request)
        {
            var creatorUserName = _filter.UserEmail ?? "System";

            _ = Guid.TryParse(_filter.UserId, out Guid creatorUserId);

            // 1) Ensure Resource Exists
            var resourceRepo = _unitOfWork.Repository<OESResource, long>();
            var resourceName = request.ResourceType.ToString();
            var resource = await resourceRepo.GetObjAsync(r => r.Name == resourceName && !r.IsDeleted);
            if (resource == null)
            {
                resource = new OESResource
                {
                    Name = resourceName,
                    Description = $"Auto-generated resource for managing {request.ResourceType}.",
                    CreationUser = creatorUserName
                };
                await resourceRepo.AddAsync(resource);
            }

            // 2) Create Owner Group
            bool isSuperAdmin = _filter.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin) || _filter.SsoUserRoles.Exists(x => x.Name == AdminRoles.Entity_Admin);

            if (isSuperAdmin)
            {
                await _unitOfWork.Complete();
                return new AutoPermissionAssignmentResponse
                {
                    AutoCreatedGroupId = Guid.Empty,
                    ResourceId = resource.Id
                };
            }

            var groupRepo = _unitOfWork.Repository<OESGroup, Guid>();
            var ownerGroup = new OESGroup
            {
                Name = $"{creatorUserName}-{request.ResourceType}-Group-{DateTimeHelper.Now.Ticks}",
                Description = $"Auto-created owner group for {request.ResourceType}: {request.EntityName}",
                AutoCreatedForUser = true,
                IsTemplate = false,
                IsPredefined = false,
                CreationUser = creatorUserName
            };
            await groupRepo.AddAsync(ownerGroup);

            // 3) Link Entity → Owner Group
            var entityLink = CreateEntityGroupLink(
                request.EntityGroupType,
                request.EntityId,
                ownerGroup.Id
            );
            await _addEntityGroupMap[request.EntityGroupType](entityLink);

            // 4) Link creator user → Owner group
            if (creatorUserId != Guid.Empty)
            {
                var userGroupRepo = _unitOfWork.Repository<AppUserProfileGroup, long>();

                await userGroupRepo.AddAsync(new AppUserProfileGroup
                {
                    AppUserProfileId = creatorUserId,
                    OESGroupId = ownerGroup.Id,
                    CreationUser = creatorUserName
                });
            }

            // 5) Link Additional Groups
            if (request.AdditionalGroupIds is not null)
            {
                foreach (var groupId in request.AdditionalGroupIds)
                {
                    var extraLink = CreateEntityGroupLink(
                        request.EntityGroupType,
                        request.EntityId,
                        groupId);

                    await _addEntityGroupMap[request.EntityGroupType](extraLink);
                }
            }

            // 6) Create GroupResource
            var groupResourceRepo = _unitOfWork.Repository<OESGroupResource, long>();
            var groupResource = new OESGroupResource
            {
                GroupId = ownerGroup.Id,
                ResourceId = resource.Id,
                ResourceType = request.ResourceType,
                CreationUser = creatorUserName
            };
            await groupResourceRepo.AddAsync(groupResource);
            await _unitOfWork.Complete();

            // 7) Assign Module Roles
            var rolePrefix = request.ResourceType.GetRolePrefix();
            var roleRepo = _unitOfWork.Repository<OESRole, Guid>();
            var moduleRoles = await roleRepo
                .GetAll()
                .Where(r => r.Name.StartsWith(rolePrefix))
                .ToListAsync();
            var groupResourceRoleRepo = _unitOfWork.Repository<OESGroupResourceRole, long>();
            var groupRoleRepo = _unitOfWork.Repository<OESGroupRole, long>();

            foreach (var role in moduleRoles)
            {
                await groupResourceRoleRepo.AddAsync(new OESGroupResourceRole
                {
                    GroupResourceId = groupResource.Id,
                    RoleId = role.Id,
                    CreationUser = creatorUserName
                });
                await groupRoleRepo.AddAsync(new OESGroupRole
                {
                    OESGroupId = ownerGroup.Id,
                    OESRoleId = role.Id,
                    CreationUser = creatorUserName
                });
            }

            await _unitOfWork.Complete();

            return new AutoPermissionAssignmentResponse
            {
                AutoCreatedGroupId = ownerGroup.Id,
                ResourceId = resource.Id
            };
        }

        #region Helper Methods
        private static object CreateEntityGroupLink(Type entityGroupType, long entityId, Guid groupId)
        {
            if (entityGroupType == typeof(ItemBankGroups))
                return new ItemBankGroups { ItemBankId = entityId, OESGroupId = groupId };

            if (entityGroupType == typeof(QuestionGroups))
                return new QuestionGroups { QuestionId = entityId, OESGroupId = groupId };

            if (entityGroupType == typeof(ILOGroup))
                return new ILOGroup { ILOId = entityId, GroupId = groupId };

            if (entityGroupType == typeof(PaperGroups))
                return new PaperGroups { PaperId = entityId, OESGroupId = groupId };

            if (entityGroupType == typeof(ScheduleGroups))
                return new ScheduleGroups { ScheduleId = entityId, OESGroupId = groupId };

            if (entityGroupType == typeof(BlockGroups))
                return new BlockGroups { BlockId = entityId, OESGroupId = groupId };

            throw new InvalidOperationException(
                $"{Resource.UnknownEntity}: {entityGroupType.Name}");
        }
        #endregion
    }
}
