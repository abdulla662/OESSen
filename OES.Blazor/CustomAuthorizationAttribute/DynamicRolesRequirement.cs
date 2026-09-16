using Microsoft.AspNetCore.Authorization;

namespace OES.Blazor.CustomAuthorizationAttribute
{
    public class DynamicRolesRequirement : IAuthorizationRequirement
    {
        public string PageName { get; }

        public DynamicRolesRequirement(string pageName)
        {
            PageName = pageName;
        }
    }
}
