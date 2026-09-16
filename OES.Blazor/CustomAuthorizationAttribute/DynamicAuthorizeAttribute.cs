using Microsoft.AspNetCore.Authorization;

namespace OES.Blazor.CustomAuthorizationAttribute
{
    public class DynamicAuthorizeAttribute : AuthorizeAttribute
    {
        public DynamicAuthorizeAttribute(string pageName)
        {
            Policy = pageName;
        }
    }
}
