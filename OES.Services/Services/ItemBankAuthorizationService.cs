using OES.Core.Entities;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using SharedHelper.RolesNames;
using System.Net;

namespace OES.Services.Services
{
    public class ItemBankAuthorizationService : IItemBankAuthorizationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly FilterParamsValues _filterParamsValues;

        public ItemBankAuthorizationService(
            IUnitOfWork unitOfWork,
            FilterParamsValues filterParamsValues
        )
        {
            _unitOfWork = unitOfWork;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<bool> CanUseItemBankAsync(long itemBankId)
        {
            if (_filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin))
            {
                return true;
            }

            var userGroupIds = _filterParamsValues
                .OesUserGroupsAndRoles
                .Where(x => x.GroupRoles.Any(r => r.Name == OesTemplateRoleConstants.ItemBankQuestionCreator))
                .Select(x => x.GroupId)
                .ToList();

            if (userGroupIds.Count == 0)
                return false;

            return await _unitOfWork
                .Repository<ItemBankGroups, long>()
                .IsExistAsync(x =>
                    x.ItemBankId == itemBankId &&
                    userGroupIds.Contains(x.OESGroupId) &&
                    !x.IsDeleted
                );
        }

        public async Task<ApiResponse> CanDoQuestionActionAsync(long itemBankId, string requiredRole)
        {
            var apiResponse = new ApiResponse();

            if (_filterParamsValues.SsoUserRoles.Any(r =>
                r.Name == AdminRoles.SuperAdmin ||
                r.Name == AdminRoles.Entity_Admin))
            {
                return apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.Authorized
                );
            }

            var userGroupIds = _filterParamsValues
                .OesUserGroupsAndRoles
                .Select(x => x.GroupId)
                .ToHashSet();

            if (userGroupIds.Count == 0)
            {
                return apiResponse.GetApiResponse(
                    CustomCodeStatus.Forbidden,
                    HttpStatusCode.Forbidden,
                    Resource.Authorized
                );
            }

            var itemBankGroupIds = await _unitOfWork
                .Repository<ItemBankGroups, long>()
                .GetAllAsync(x =>
                    x.ItemBankId == itemBankId &&
                    !x.IsDeleted &&
                    userGroupIds.Contains(x.OESGroupId)
                );

            if (!itemBankGroupIds.Any())
            {
                return apiResponse.GetApiResponse(
                    CustomCodeStatus.Forbidden,
                    HttpStatusCode.Forbidden,
                    Resource.NotAuthorized
                );
            }

            var matchingGroupIds = itemBankGroupIds
                .Select(x => x.OESGroupId)
                .ToHashSet();

            var hasRole = _filterParamsValues
                .OesUserGroupsAndRoles
                .Where(x => matchingGroupIds.Contains(x.GroupId))
                .SelectMany(x => x.GroupRoles)
                .Any(r => r.Name == requiredRole);

            if (!hasRole)
            {
                return apiResponse.GetApiResponse(
                    CustomCodeStatus.Forbidden,
                    HttpStatusCode.Forbidden,
                    Resource.NotAuthorized
                );
            }

            return apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Authorized
            );
        }
    }
}
