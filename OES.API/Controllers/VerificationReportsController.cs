using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Results;
using OES.Interface.Interfaces;
using System.Text.Json;

namespace OES.API.Controllers
{
    public class VerificationReportsController : OESBaseController
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IVerificationReportsService _verificationReportsService;

        public VerificationReportsController(IVerificationReportsService verificationReportsService)
        {
            _verificationReportsService = verificationReportsService;
        }

        [HttpGet("GetVenueCount")]
        [OESFilter(Authorize = true)]
        public async Task<IActionResult> GetVenueCountAsync(CancellationToken cancellationToken = default)
        {
            var count = await _verificationReportsService.GetVenueCountAsync();

            return Ok(count);
        }

        [HttpPost("GetCentersAllocationVerificationReport")]
        [OESFilter(Authorize = true)]
        public async Task GetCentersAllocationVerificationReportAsync([FromBody] CentersAllocationVerificationRequest request, CancellationToken cancellationToken = default)
        {
            ConfigureStreamingResponse();

            await foreach (var result in _verificationReportsService.GetCentersAllocationVerificationReportStreamAsync(request, cancellationToken))
            {
                await WriteNdjsonAsync(result, cancellationToken);
            }
        }

        [HttpPost("GetVenueAttendanceVerificationReport")]
        [OESFilter(Authorize = true)]
        public async Task GetVenueAttendanceVerificationReportAsync([FromBody] VenueAttendanceVerificationRequest request, CancellationToken cancellationToken = default)
        {
            ConfigureStreamingResponse();

            await foreach (var result in _verificationReportsService.GetVenueAttendanceVerificationReportStreamAsync(request, cancellationToken))
            {
                await WriteNdjsonAsync(result, cancellationToken);
            }
        }


        #region Helper Methods

        private void ConfigureStreamingResponse()
        {
            Response.ContentType = "application/x-ndjson; charset=utf-8";

            HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        }

        private async Task WriteNdjsonAsync<T>(T data, CancellationToken cancellationToken)
        {
            await Response.WriteAsync(JsonSerializer.Serialize(data, JsonOptions) + "\n", cancellationToken);

            await Response.Body.FlushAsync(cancellationToken);
        }

        #endregion
    }
}