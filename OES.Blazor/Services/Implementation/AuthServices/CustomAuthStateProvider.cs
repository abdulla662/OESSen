using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace OES.Blazor.Services.Implementation.AuthServices
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
        private const string Bearer = nameof(Bearer);

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        public void AddClaims(IEnumerable<Claim> claims)
        {
            var identity = (ClaimsIdentity)_currentUser.Identity;

            if (identity?.IsAuthenticated != true)
            {
                identity = new ClaimsIdentity(claims, Bearer);

                _currentUser = new ClaimsPrincipal(identity);
            }
            else
            {
                foreach (var claim in claims)
                {
                    if (!identity.HasClaim(c => c.Type == claim.Type && c.Value == claim.Value))
                    {
                        identity.AddClaim(claim);
                    }
                }
            }

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
