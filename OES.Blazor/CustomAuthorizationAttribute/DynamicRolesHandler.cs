using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.MemoryCache;
using OES.Helper.General.GlobalUserContext;
using SharedHelper.RolesNames;

namespace OES.Blazor.CustomAuthorizationAttribute
{
    public class DynamicRolesHandler : AuthorizationHandler<DynamicRolesRequirement>
    {
        private readonly IBlazMemoryCache _blazMemoryCache;
        private readonly NavigationManager _navigationManager;
        private readonly GlobalUserContext _globalUserContext;

        public DynamicRolesHandler(IBlazMemoryCache blazMemoryCache,
                                   NavigationManager navigationManager,
                                   GlobalUserContext globalUserContext)
        {
            _blazMemoryCache = blazMemoryCache;
            _navigationManager = navigationManager;
            _globalUserContext = globalUserContext;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DynamicRolesRequirement requirement)
        {
            if (_globalUserContext.UserId == Guid.Empty) // This makes sure that the global user context is filled with data from the jwt.
            {
                return;
            }

            var roles = await _blazMemoryCache.GetPageRoles(requirement.PageName); // This gets the roles from the memory cache, or from the back-end database if not available in the memory cache.

            bool passed = false;

            if (context.User.IsInRole(AdminRoles.SuperAdmin) || context.User.IsInRole(AdminRoles.Entity_Admin))
            {
                passed = true;
                context.Succeed(requirement);
            }

            if (roles == null || roles.Roles.Count == 0)
            {
                passed = true;
                context.Succeed(requirement);
            }
            else
            {
                if (roles.Roles.Exists(roleName => context.User.IsInRole(roleName)))
                {
                    passed = true;
                    context.Succeed(requirement);
                }
            }

            if (!passed)
                _navigationManager.NavigateTo("/ForbiddenPage");
        }
    }
}
