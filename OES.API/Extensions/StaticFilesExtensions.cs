using Microsoft.AspNetCore.StaticFiles;

namespace OES.API.Extensions;

public static class StaticFilesExtensions
{
    public static IApplicationBuilder UseConfiguredStaticFiles(this IApplicationBuilder app)
    {
        var contentTypeProvider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                [".wasm"] = "application/wasm",
                [".woff"] = "font/woff",
                [".woff2"] = "font/woff2",
                [".ttf"] = "font/ttf",
                [".webmanifest"] = "application/manifest+json",
                [".webp"] = "image/webp",
                [".avif"] = "image/avif"
            }
        };

        return app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypeProvider
        });
    }
}

