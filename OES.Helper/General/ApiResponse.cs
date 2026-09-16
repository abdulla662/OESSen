using OES.Helper.Enums;
using OES.Helper.Interfaces;
using System.Net;

namespace OES.Helper.General
{
    public class ApiResponse : IApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }

        public CustomCodeStatus CustomCodeStatus { get; set; }

        public string Message { get; set; }

        public object Data { get; set; }

        public ApiResponse GetApiResponse()
        {
            return new ApiResponse()
            {
                StatusCode = HttpStatusCode.OK
            };
        }

        public ApiResponse GetApiResponse(CustomCodeStatus errorNumber, HttpStatusCode httpStatusCode, string message = null, object data = null)
        {
            return new ApiResponse()
            {
                CustomCodeStatus = errorNumber,
                StatusCode = httpStatusCode,
                Message = message ?? Enum.GetName(typeof(CustomCodeStatus), errorNumber)!,
                Data = data
            };
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
