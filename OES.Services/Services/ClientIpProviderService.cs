using Microsoft.AspNetCore.Http;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.Services.Services
{
    public class ClientIpProviderService : IClientIpProviderService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientIpProviderService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;

            return context?.Request.Headers[MiscConstants.XOriginalClientIpHeader].FirstOrDefault()
                   ?? context?.Request.Headers[MiscConstants.CfConnectingIpHeader].FirstOrDefault()
                   ?? context?.Request.Headers[MiscConstants.XForwardedForHeader].FirstOrDefault()?.Split(',')[0].Trim()
                   ?? context?.Connection.RemoteIpAddress?.MapToIPv4().ToString()
                   ?? string.Empty;
        }
    }
}