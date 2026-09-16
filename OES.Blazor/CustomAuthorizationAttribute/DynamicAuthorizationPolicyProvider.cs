using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace OES.Blazor.CustomAuthorizationAttribute
{
    public class DynamicAuthorizationPolicyProvider : IAuthorizationPolicyProvider
    {
        public DefaultAuthorizationPolicyProvider DefaultAuthorizationPolicyProvider { get; }

        public DynamicAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            DefaultAuthorizationPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => DefaultAuthorizationPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => DefaultAuthorizationPolicyProvider.GetFallbackPolicyAsync();

        public async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            var policy = new AuthorizationPolicyBuilder();

            policy.Requirements.Add(new DynamicRolesRequirement(policyName));

            return await Task.FromResult(policy.Build());
        }
    }
}
