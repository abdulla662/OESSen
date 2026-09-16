using OES.Helper.Interfaces;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using SharedHelper.General;

namespace OES.API.MiddleWare
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;
        private readonly LinkGenerator _linkGenerator;
        private readonly IApiResponse _apiResponse;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment, LinkGenerator linkGenerator, IApiResponse apiResponse)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
            _linkGenerator = linkGenerator;
            _apiResponse = apiResponse;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Stopwatch stopwatch = null;

            if (_environment.IsDevelopment())
            {
                // Start a stopwatch to measure the time taken to process the request
                stopwatch = Stopwatch.StartNew();
            }

            try
            {
                await _next(context);

                if (_environment.IsDevelopment() && stopwatch != null)
                {
                    stopwatch.Stop();

                    _logger.LogInformation($@"
                        ***************************************************
                         REQUEST TIMING LOG 
                         Path: {context.Request.Path}
                         Time Taken: {stopwatch.ElapsedMilliseconds} ms
                         Timestamp: {DateTimeHelper.Now:yyyy-MM-dd HH:mm:ss}
                        ***************************************************
                    ");
                }
            }
            catch (Exception ex)
            {
                const string str = "==========================================\n";

                _logger.LogError(ex, "\n*********************************\n" + str + "Date and Time :\n" + DateTimeHelper.Now + str + "Message:\n" + ex.Message + "\n" + str + "Inner Exception :\n" + ex.InnerException + str + "StackTrace:\n" + ex.StackTrace);

                context.Response.ContentType = "application/json";

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = _environment.IsDevelopment()
                    ? _apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, ex.Message, ex.StackTrace.ToString())
                    : _apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, ex.Message);

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                var json = JsonSerializer.Serialize(response, options);

                var statusCode = (int)HttpStatusCode.InternalServerError;

                var errorUrl = _environment.IsDevelopment() ? _linkGenerator.GetPathByAction("Index", "Error", new { statusCode, detailes = ex.StackTrace }) : _linkGenerator.GetPathByAction("Index", "Error", new { statusCode });

                context.Response.Redirect(errorUrl);

                await context.Response.WriteAsync(json);
            }
        }
    }
}
