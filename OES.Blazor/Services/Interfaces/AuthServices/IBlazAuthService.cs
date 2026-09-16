using OES.Helper.Dtos.User;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.General;
using System.IdentityModel.Tokens.Jwt;

namespace OES.Blazor.Services.Interfaces.AuthServices
{
    public interface IBlazAuthService
    {
        Task HandleAuthenticationProcessForUserAsync();

        Task<string> GetDecryptedTokenFromLocalStorageAsync();

        JwtSecurityToken DecodeToken(string token);

        Task SaveEncryptedTokenToLocalStorageAsync(string encryptedToken);

        Task<bool> IsTokenExpiredOrRemovedAsync();

        void LoginWithSaml();

        Task<ApiResponse> LogInAsync(LoginDto loginDto);

        void CompleteSamlLogin(long organizationId, string moduleName);

        Task LogOutAsync();

        Task<bool> IsCurrentUserAuthorizedAsync<TGroupDto, TKey>(
            object entityId,
            string targetRoleName,
            Func<long, Task<TGroupDto>> getGroupsFunc,
            Func<TGroupDto, IEnumerable<TKey>> groupsSelector,
            Func<UserGroupsAndRolesDto, TKey> userKeySelector
        );

        Task<bool> IsCurrentUserAuthorizedForAnyRoleAsync<TGroupDto, TKey>(
            object entityId,
            IEnumerable<string> targetRoleNames,
            Func<long, Task<TGroupDto>> getGroupsFunc,
            Func<TGroupDto, IEnumerable<TKey>> groupsSelector,
            Func<UserGroupsAndRolesDto, TKey> userKeySelector
        );

        Task<bool> IsCurrentUserOwnerAsync<TDto>(
            long entityId,
            Func<long, Task<TDto>> getGroupsDtoFunc,
            Func<TDto, List<Guid>> extractGroupsIds
        );

        Task<bool> IsCurrentUserInRoleAsync(string targetRoleName);

        public Task RefreshUserContextAsync();
    }
}
