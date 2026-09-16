using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Helper.Dtos.User;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.PagesEndpointsRolesDtos;
using SharedHelper.General;
using SharedHelper.RolesNames;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.AuthServices
{
    public class BlazAuthService : IBlazAuthService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly NavigationManager _navigationManager;
        private readonly GlobalUserContext _globalUserContext;
        private readonly CustomAuthStateProvider _customAuthStateProvider;
        private const string token = nameof(token);

        private bool IsCurrentUserSuperAdminOrEntityAdmin =>
            _globalUserContext.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin || x.Name == AdminRoles.Entity_Admin);

        public BlazAuthService(ILocalStorageService localStorageService,
                               IHttpClientHelper httpClientHelper,
                               NavigationManager navigationManager,
                               GlobalUserContext globalUserContext,
                               CustomAuthStateProvider customAuthStateProvider)
        {
            _localStorageService = localStorageService;
            _httpClientHelper = httpClientHelper;
            _navigationManager = navigationManager;
            _globalUserContext = globalUserContext;
            _customAuthStateProvider = customAuthStateProvider;
        }

        public async Task HandleAuthenticationProcessForUserAsync()
        {
            // Step 1: Get the JWT from the local storage of this origin for the current user:
            var _token = await GetDecryptedTokenFromLocalStorageAsync();

            // Step 2: If the token is null or empty, then navigate to the login page:
            if (string.IsNullOrWhiteSpace(_token))
            {
                _navigationManager.NavigateTo("/login");
                return;
            }

            // Step 3: Validate the token in the evaluation system back end:
            var validationResult = await ValidateTokenInBackEndAsync(_token);

            // Step 4: Check if the token is valid...
            if (validationResult.IsValid)
            {
                // Step 5: Since the token is valid, then decode it:
                var decodedToken = DecodeToken(_token);

                // Step 6: Fill the global user context from the decoded token and session storage:
                FillGlobalUserContextFromToken(decodedToken);

                // Step 7: Fill the rest of the global user context from the OES system itself:
                await FillRestOfGlobalUserContextFromOesSystemAsync();

                // Step 8: Add the claims (including user roles) to the authentication state provider:
                AddAllUserRolesToUserIdentityClaims();
            }
            else
            {
                // Step 5: Since the token is invalid, then clear the global user context:
                EmptyGlobalUserContext();

                // Step 6: Then navigate to the login page:
                _navigationManager.NavigateTo("/login");
            }
        }

        public async Task<string> GetDecryptedTokenFromLocalStorageAsync()
        {
            return await _localStorageService.GetItemAsStringAsync(token);
        }

        public async Task SaveEncryptedTokenToLocalStorageAsync(string encryptedToken)
        {
            await _localStorageService.SetItemAsStringAsync("token", encryptedToken);
        }

        public JwtSecurityToken DecodeToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (tokenHandler.CanReadToken(token))
                return tokenHandler.ReadJwtToken(token);

            return null;
        }

        public async Task<bool> IsTokenExpiredOrRemovedAsync()
        {
            var jwt = await GetDecryptedTokenFromLocalStorageAsync();

            if (!string.IsNullOrWhiteSpace(jwt))
            {
                var decodedToken = DecodeToken(jwt);

                var currentTime = DateTimeHelper.Now;

                return decodedToken!.ValidTo <= currentTime;
            }

            return true;
        }

        public void CompleteSamlLogin(long organizationId, string moduleName)
        {
            _navigationManager.NavigateTo(
                $"{CentralizedUrlHelper.SsoApiBaseUrl}api/Account/CompleteSaml" +
                $"?organizationId={organizationId}" +
                $"&moduleName={moduleName}"
            );
        }

        public void LoginWithSaml()
        {
            var url = $"{CentralizedUrlHelper.SsoApiBaseUrl}api/Account/ChallengeSaml?moduleName={CentralizedUrlHelper.OesModuleName}";

            _navigationManager.NavigateTo(url);
        }

        public async Task<ApiResponse> LogInAsync(LoginDto loginDto)
        {
            var response = await _httpClientHelper.PostAsync(loginDto, "api/UserProfile/LogIn");

            return response;
        }

        public async Task LogOutAsync()
        {
            var isSaml = _globalUserContext.CurrentUserAuthenticationType == AuthenticationType.SamlBased;

            EmptyGlobalUserContext();

            await _localStorageService.RemoveItemAsync(token);

            if (isSaml)
            {
                var logoutUrl =
                       $"{CentralizedUrlHelper.SsoApiBaseUrl}api/Account/LogoutSaml" +
                       $"?moduleName={CentralizedUrlHelper.OesModuleName}";

                _navigationManager.NavigateTo(logoutUrl);
            }
            else
            {
                await HandleAuthenticationProcessForUserAsync();
            }
        }

        public async Task<bool> IsCurrentUserAuthorizedAsync<TGroupDto, TKey>(
            object entityId,
            string targetRoleName,
            Func<long, Task<TGroupDto>> getGroupsFunc,
            Func<TGroupDto, IEnumerable<TKey>> groupsSelector,
            Func<UserGroupsAndRolesDto, TKey> userKeySelector
        )
        {
            if (IsCurrentUserSuperAdminOrEntityAdmin)
                return true;

            var dto = await getGroupsFunc((long)entityId);

            var entityKeys = groupsSelector(dto);

            var matchedGroups = _globalUserContext
                .OesUserGroupsAndRoles
                .IntersectBy(entityKeys, userKeySelector);

            var roles = matchedGroups
                .SelectMany(g => g.GroupRoles)
                .Select(r => r.Name);

            return roles.Contains(targetRoleName);
        }

        public async Task<bool> IsCurrentUserAuthorizedForAnyRoleAsync<TGroupDto, TKey>(
            object entityId,
            IEnumerable<string> targetRoleNames,
            Func<long, Task<TGroupDto>> getGroupsFunc,
            Func<TGroupDto, IEnumerable<TKey>> groupsSelector,
            Func<UserGroupsAndRolesDto, TKey> userKeySelector)
        {
            if (IsCurrentUserSuperAdminOrEntityAdmin)
                return true;

            var dto = await getGroupsFunc((long)entityId);

            var entityKeys = groupsSelector(dto);

            var matchedGroups = _globalUserContext
                .OesUserGroupsAndRoles
                .IntersectBy(entityKeys, userKeySelector);

            var roles = matchedGroups
                .SelectMany(g => g.GroupRoles)
                .Select(r => r.Name);

            return roles.Any(targetRoleNames.Contains);
        }

        public async Task<bool> IsCurrentUserOwnerAsync<TDto>(
            long entityId,
            Func<long, Task<TDto>> getGroupsDtoFunc,
            Func<TDto, List<Guid>> extractGroupsIds
        )
        {
            if (IsCurrentUserSuperAdminOrEntityAdmin)
                return true;

            var dto = await getGroupsDtoFunc(entityId);

            if (object.Equals(dto, default(TDto)))
                return false;

            var groupsIds = extractGroupsIds(dto);

            if (groupsIds == null || groupsIds.Count == 0)
                return false;

            var ownerGroupId = groupsIds[0];

            var userGroups = _globalUserContext.OesUserGroupsAndRoles
                .Select(x => x.GroupId)
                .ToHashSet();

            return userGroups.Contains(ownerGroupId);
        }

        public async Task<bool> IsCurrentUserInRoleAsync(string targetRoleName)
        {
            if (IsCurrentUserSuperAdminOrEntityAdmin)
                return true;

            return _globalUserContext
                .OesUserGroupsAndRoles
                .SelectMany(x => x.GroupRoles)
                .Any(r => r.Name == targetRoleName);
        }

        #region Helper Methods
        private async Task<List<UserGroupsAndRolesDto>> GetOesUserGroupsAndRolesAsync(Guid userId)
        {
            var apiResponse = await _httpClientHelper.GetAsync<List<UserGroupsAndRolesDto>>($"api/UserProfile/GetUserGroupsAndRoles?userId={userId}");

            return (List<UserGroupsAndRolesDto>)apiResponse.Data;
        }

        private void AddAllUserRolesToUserIdentityClaims()
        {
            var rolesClaims = new List<Claim>();

            _globalUserContext.SsoUserRoles.ForEach(role => rolesClaims.Add(new Claim(ClaimTypes.Role, role.Name)));

            _globalUserContext.OesUserRoles.ForEach(role => rolesClaims.Add(new Claim(ClaimTypes.Role, role.Name)));

            _customAuthStateProvider.AddClaims(rolesClaims);
        }

        private async Task<TokenValidationResultDto> ValidateTokenInBackEndAsync(string token)
        {
            var apiResponse = await _httpClientHelper.GetAsync<TokenValidationResultDto>($"api/UserProfile/ValidateTokenForUser?token={token}");

            return (TokenValidationResultDto)apiResponse.Data;
        }

        private void FillGlobalUserContextFromToken(JwtSecurityToken jwtSecurityToken)
        {
            if (jwtSecurityToken is not null)
            {
                var jtiClaimValue = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.Jti)?.Value;
                _globalUserContext.JsonTokenIdentifier = Guid.TryParse(jtiClaimValue, out Guid parsedJti) ? parsedJti : Guid.NewGuid();

                var userIdClaimValue = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.ModuleUserID)?.Value;
                _globalUserContext.UserId = Guid.TryParse(userIdClaimValue, out Guid parsedUserId) ? parsedUserId : Guid.NewGuid();

                var organizationIdClaimValue = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.OrganizationId)?.Value;
                _globalUserContext.CurrentOrganizationId = long.TryParse(organizationIdClaimValue, out long parsedOrganizationId) ? parsedOrganizationId : 0;

                var moduleKeyClaimValue = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.ModuleKey)?.Value;
                _globalUserContext.CurrentModuleKey = Guid.TryParse(moduleKeyClaimValue, out Guid parsedModuleId) ? parsedModuleId : Guid.NewGuid();

                var allowedOrganizationsStringified = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.AllowedOrganizations)?.Value;
                _globalUserContext.AllowedOrganizations = allowedOrganizationsStringified is not null ? JsonSerializer.Deserialize<List<AllowedOrganization>>(allowedOrganizationsStringified) : [];

                var ssoUserGroupsStringified = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.SsoUserGroups)?.Value;
                _globalUserContext.SsoUserGroups = ssoUserGroupsStringified is not null ? JsonSerializer.Deserialize<List<UserGroup>>(ssoUserGroupsStringified) : [];

                var ssoUserRolesStringified = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.SsoUserRoles)?.Value;

                _globalUserContext.SsoUserRoles = ssoUserRolesStringified is not null ? JsonSerializer.Deserialize<List<UserRole>>(ssoUserRolesStringified) : [];

                _globalUserContext.UserName = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.UserName)?.Value;
                _globalUserContext.UserEmail = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.UserEmail)?.Value;
                _globalUserContext.UserDeviceIpAddress = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.DeviceIpAddress)?.Value;
                _globalUserContext.OrganizationModuleSubscriptionKey = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.SubscriptionKey)?.Value;
                _globalUserContext.CurrentOrganizationSignature = jwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.OrganizationSignature)?.Value;

                _globalUserContext.CurrentUserAuthenticationType = Enum.TryParse<AuthenticationType>(
                    jwtSecurityToken
                    .Claims
                    .FirstOrDefault(claim => claim.Type == CustomJwtClaimsTypes.UserAuthenticationType)?.Value, out var userAuthType)
                        ? userAuthType
                        : AuthenticationType.SystemBased;
            }
        }

        private async Task FillRestOfGlobalUserContextFromOesSystemAsync()
        {
            var oesUserGroupsAndRolesList = await GetOesUserGroupsAndRolesAsync(_globalUserContext.UserId);

            if (oesUserGroupsAndRolesList != null && oesUserGroupsAndRolesList.Count != 0)
            {
                _globalUserContext.OesUserGroupsAndRoles = oesUserGroupsAndRolesList;

                _globalUserContext.OesUserRoles = [.. oesUserGroupsAndRolesList.SelectMany(x => x.GroupRoles)];
            }

            var pagesResponse = await _httpClientHelper.GetAsync<List<PageRoleDTO>>("api/Roles/GetAllPagesRoles");

            if (pagesResponse?.Data is List<PageRoleDTO> pages)
                _globalUserContext.UserAccessiblePages = pages;
        }

        public async Task RefreshUserContextAsync()
        {
            await FillRestOfGlobalUserContextFromOesSystemAsync();

            AddAllUserRolesToUserIdentityClaims();
        }

        private void EmptyGlobalUserContext()
        {
            _globalUserContext.JsonTokenIdentifier = Guid.Empty;
            _globalUserContext.UserId = Guid.Empty;
            _globalUserContext.UserName = string.Empty;
            _globalUserContext.UserEmail = string.Empty;
            _globalUserContext.UserDeviceIpAddress = string.Empty;
            _globalUserContext.OrganizationModuleSubscriptionKey = string.Empty;
            _globalUserContext.CurrentOrganizationId = 0;
            _globalUserContext.CurrentOrganizationSignature = string.Empty;
            _globalUserContext.CurrentUserAuthenticationType = AuthenticationType.SystemBased;
            _globalUserContext.CurrentModuleKey = Guid.Empty;
            _globalUserContext.AllowedOrganizations = [];
            _globalUserContext.SsoUserGroups = [];
            _globalUserContext.SsoUserRoles = [];
            _globalUserContext.OesUserGroupsAndRoles = [];
            _globalUserContext.OesUserRoles = [];
        }
        #endregion
    }
}
