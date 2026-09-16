using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using System.Net;

namespace OES.Services.Services
{
    public sealed class UserSyncService : IUserSyncService
    {
        private readonly IUserProfileGRPCService _userProfileGRPCService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApiResponse _apiResponse;
        private readonly IMapper _mapper;
        private readonly FilterParamsValues _filterParamsValues;

        public UserSyncService(IUserProfileGRPCService userProfileGRPCService, IMapper mapper, IUnitOfWork unitOfWork, IApiResponse apiResponse, FilterParamsValues filterParamsValues)
        {
            _userProfileGRPCService = userProfileGRPCService;
            _unitOfWork = unitOfWork;
            _apiResponse = apiResponse;
            _mapper = mapper;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<IApiResponse> SyncUsers()
        {
            var userParamsForSync = new UserParamsForSync
            {
                ModuleId = _filterParamsValues.ModuleId.ToString(),
                OrganizationId = _filterParamsValues.OrganizationId.ToString(),
                OrganizationSignature = _filterParamsValues.Signature
            };

            var ssoUsersresponse = await GetAllUsersFromSSO(userParamsForSync) ?? throw new Exception("Failed to get users from SSO");

            var ssoUsers = _mapper.Map<IEnumerable<AppUserProfile>>(ssoUsersresponse);

            if (!ssoUsers.Any())
            {
                return _apiResponse.GetApiResponse(CustomCodeStatus.UserNotFound, HttpStatusCode.OK, "No users found to be synced");
            }

            var Users = _unitOfWork
                .Repository<AppUserProfile, Guid>()
                .GetAll()
                .IgnoreQueryFilters()
                .AsNoTracking();

            var ssoUserDict = ssoUsers.GroupBy(u => u.Id).ToDictionary(g => g.Key, g => g.First());
            var usersToUpdate = new List<AppUserProfile>();
            var usersToAdd = new List<AppUserProfile>();

            foreach (var OesUser in Users)
            {
                if (ssoUserDict.TryGetValue(OesUser.Id, out var ssoUser))
                {
                    if (!UserProfilesAreEqual(ssoUser, OesUser))
                    {
                        usersToUpdate.Add(ssoUser);
                    }

                    ssoUserDict.Remove(OesUser.Id); // Mark this user as handled in the dictionary
                }
            }

            usersToAdd.AddRange(ssoUserDict.Values);

            if (usersToUpdate.Count == 0 && usersToAdd.Count == 0)
            {
                return _apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.NoChangesToApply);
            }

            await ApplyUserChanges(usersToUpdate, usersToAdd);

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                               HttpStatusCode.OK,
                                               Resource.UserSyncedSuccessfully);
        }

        private async Task<IEnumerable<AppUserProfileRetrievalDto>> GetAllUsersFromSSO(UserParamsForSync userParamsForSync)
        {
            var response = await _userProfileGRPCService.GetAllOrganizationUserProfile(userParamsForSync);

            if (response.CustomCodeStatus != CustomCodeStatus.Success)
            {
                throw new Exception($"Failed to get users from SSO. Status: {response.CustomCodeStatus}");
            }

            var userProfiles = (IEnumerable<AppUserProfileRetrievalDto>)response.Data;

            if (userProfiles.Any())
            {
                return userProfiles;
            }

            throw new InvalidCastException($"Failed to cast response.Data to IEnumerable<AppUserProfile>. Actual type: {response.Data?.GetType()}");
        }

        private static bool UserProfilesAreEqual(AppUserProfile ssoUser, AppUserProfile evaluationUser)
        {
            return ssoUser.Username == evaluationUser.Username &&
                   ssoUser.EmailAddress == evaluationUser.EmailAddress &&
                   ssoUser.IsDeleted == evaluationUser.IsDeleted &&
                   ssoUser.IsActive == evaluationUser.IsActive;
        }

        private async Task ApplyUserChanges(IEnumerable<AppUserProfile> usersToUpdate, IEnumerable<AppUserProfile> usersToAdd)
        {
            if (usersToUpdate.Any())
            {
                _unitOfWork.Repository<AppUserProfile, Guid>().UpdateRange([.. usersToUpdate]);
            }

            if (usersToAdd.Any())
            {
                _unitOfWork.Repository<AppUserProfile, Guid>().AddRangAsync([.. usersToAdd]);
            }

            await _unitOfWork.Complete();
        }
    }
}
