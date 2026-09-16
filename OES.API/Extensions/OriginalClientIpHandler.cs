using OES.Helper.General;

namespace OES.API.Extensions
{
    public class OriginalClientIpHandler : DelegatingHandler
    {
        public const string HeaderName = MiscConstants.XOriginalClientIpHeader;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public OriginalClientIpHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;

            var clientIp = context?.Request.Headers[MiscConstants.CfConnectingIpHeader].FirstOrDefault()
                ?? context?.Connection.RemoteIpAddress?.MapToIPv4().ToString();

            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                request.Headers.Remove(HeaderName);
                request.Headers.TryAddWithoutValidation(HeaderName, clientIp);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}