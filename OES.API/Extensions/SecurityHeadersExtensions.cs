using OES.Helper.General;

namespace OES.API.Extensions;

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[MiscConstants.XContentTypeOptionsHeader] = MiscConstants.XContentTypeOptionsHeaderValue;
                return Task.CompletedTask;
            });

            await next();
        });
    }
}

