using System.Net;
using System.Text.Json.Serialization;

namespace OES.Helper.General.NewApiResponse
{
    public class NewApiResponse<T>
    {
        public bool Success { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public string Message { get; private set; }
        public T? Data { get; private set; }
        public List<string> Errors { get; private set; }

        [JsonConstructor]
        private NewApiResponse(bool success, HttpStatusCode statusCode, string message, T? data, List<string> errors)
        {
            Success = success;
            StatusCode = statusCode;
            Message = message;
            Data = data;
            Errors = errors;
        }

        public static NewApiResponse<T> EmptyResponse()
        {
            return new NewApiResponse<T>(false, HttpStatusCode.BadRequest, string.Empty, default, default);
        }
    }
}
