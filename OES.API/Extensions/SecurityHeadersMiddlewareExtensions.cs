namespace OES.API.Extensions
{
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseContentSecurityPolicy(
            this IApplicationBuilder app,
            bool reportOnly = true)
        {
            var directives = new[]
            {
                "default-src 'self'",
                "script-src 'self' 'wasm-unsafe-eval'",
                "style-src 'self' 'unsafe-inline'",
                "img-src 'self' data: https:",
                "font-src 'self' data:",
                "connect-src 'self' wss:",
                "object-src 'none'",
                "base-uri 'self'",
                "form-action 'self'",
                "frame-ancestors 'none'",
                "upgrade-insecure-requests"
            };

            var headerName = reportOnly
                ? "Content-Security-Policy-Report-Only"
                : "Content-Security-Policy";

            return app.Use(async (context, next) =>
            {
                context.Response.Headers[headerName] = string.Join("; ", directives);
                await next();
            });
        }
    }
}
