using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.User;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class UserProfileController : OESBaseController
    {
        private readonly IUserProfileService _userProfileService;
        private readonly IUserProfileGRPCService _userProfileGRPCService;
        private readonly IUserSyncService _userSyncService;

        public UserProfileController(IUserProfileService userProfileService,
                                     IUserProfileGRPCService userProfileGRPCService,
                                     IUserSyncService userSyncService)
        {
            _userProfileService = userProfileService;
            _userProfileGRPCService = userProfileGRPCService;
            _userSyncService = userSyncService;
        }

        /// <summary>
        /// Validates the provided token for the user.
        /// </summary>
        /// <param name="token">The token that needs to be validated for the user.</param>
        /// <remarks>
        /// This endpoint verifies whether the given token is valid for the user,
        /// typically used for authentication or session management purposes.
        /// </remarks>
        /// <returns>Returns an ApiResponse indicating the result of the token validation process.</returns>
        [OESFilter(Authorize = false)]
        [HttpGet("ValidateTokenForUser")]
        public async Task<IApiResponse> ValidateTokenForUserAsync(string? token)
        {
            return await _userProfileService.ValidateTokenForUserAsync(token);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetOrgainzationUserGRPC")]
        public async Task<IApiResponse> GetOrganizationUserGRPC(UserParamsForSync userParamsForSync)
        {
            return await _userProfileGRPCService.GetAllOrganizationUserProfile(userParamsForSync);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("SyncUsersProfiles")]
        public async Task<IApiResponse> SyncUsersProfilesAsync()
        {
            return await _userSyncService.SyncUsers();
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetAllUsers")]
        public async Task<ApiResponse> GetAllUsersAsync()
        {
            return await _userProfileService.GetAllAsync();
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetUserGroupsAndRoles")]
        public Task<ApiResponse> GetUserGroupsAndRolesAsync(Guid userId)
        {
            return _userProfileService.GetUserGroupsAndRolesAsync(userId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllUserPaginationAsync")]
        public async Task<ApiResponse> GetAllUserPaginationAsync(PaginationSearchModel paginationModel)
        {
            return await _userProfileService.GetAllUserPaginationAsync(paginationModel);
        }


        [HttpPost("AssignAndUnassignUserToGroup")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AssignAndUnassignUserToGroupAsync(UsersToGroupDto usersToGroupDto)
        {
            return await _userProfileService.AssignAndUnassignUserToGroupAsync(usersToGroupDto);
        }

        [DoNotEncrypt]
        [OESFilter(Authorize = false)]
        [HttpPost("LogIn")]
        public async Task<ApiResponse> LogIn(LoginDto loginDto)
        {
            return await _userProfileService.LogInAsync(loginDto);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetAssignedUserIds")]
        public async Task<ApiResponse> GetAssignedUserIds([FromQuery] Guid groupId)
        {
            return await _userProfileService.GetAssignedUserIdsByGroupAsync(groupId);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetUserAccessByResourceType")]
        public Task<ApiResponse> GetUserAccessByResourceTypeAsync(Guid userId, ResourceType resourceType)
        {
            return _userProfileService.GetUserAccessByResourceTypeAsync(userId, resourceType);
        }
    }
}
