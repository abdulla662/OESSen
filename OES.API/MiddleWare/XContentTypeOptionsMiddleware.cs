namespace OES.API.MiddleWare
{
    public class XContentTypeOptionsMiddleware
    {
        private readonly RequestDelegate _next;

        public XContentTypeOptionsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            await _next(context);
        }
    }

    public static class XContentTypeOptionsMiddlewareExtensions
    {
        public static IApplicationBuilder UseXContentTypeOptionsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<XContentTypeOptionsMiddleware>();
        }
    }
}
