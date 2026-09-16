using OES.Helper.General;
using System.Text;

namespace OES.API.MiddleWare
{
    public class EncryptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public EncryptionMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_configuration.GetValue<bool>(MiscConstants.PayloadEncryptionEnabled, false))
            {
                await _next(context);
                return;
            }

            var endpoint = context.GetEndpoint();

            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            var requiresEncryption = endpoint.Metadata.GetMetadata<EncryptedPayloadAttribute>() != null;

            var developerDisabledEncryption = context.Request.Headers.TryGetValue(MiscConstants.DeveloperDisableEncryption, out var value) &&
                                              string.Equals(value.ToString(), "true", StringComparison.OrdinalIgnoreCase);

            var isExcluded = endpoint.Metadata.GetMetadata<DoNotEncryptAttribute>() != null;

            var isMultiPart = context.Request.HasFormContentType;

            if (!requiresEncryption || developerDisabledEncryption || isExcluded || isMultiPart)
            {
                await _next(context);
                return;
            }

            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var encryptedBody = await reader.ReadToEndAsync();
            var decryptedBody = AesCipher.Decrypt(encryptedBody);

            var newRequestStream = new MemoryStream(Encoding.UTF8.GetBytes(decryptedBody));
            context.Request.Body = newRequestStream;
            context.Request.ContentLength = newRequestStream.Length;
            newRequestStream.Position = 0;

            var originalResponseBody = context.Response.Body;
            await using var newResponseStream = new MemoryStream();
            context.Response.Body = newResponseStream;

            try
            {
                await _next(context);
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }

            newResponseStream.Seek(0, SeekOrigin.Begin);

            var responseJson = await new StreamReader(newResponseStream).ReadToEndAsync();

            var encryptedResponse = AesCipher.Encrypt(responseJson);

            context.Response.ContentType = MiscConstants.ApplicationJsonContentType;

            await context.Response.WriteAsync(encryptedResponse);
        }
    }
}