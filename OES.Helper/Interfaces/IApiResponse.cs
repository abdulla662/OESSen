using OES.Helper.Enums;
using OES.Helper.General;
using System.Net;

namespace OES.Helper.Interfaces
{
    public interface IApiResponse : IDisposable
    {
        ApiResponse GetApiResponse();

        ApiResponse GetApiResponse(CustomCodeStatus errorNumber, HttpStatusCode httpStatusCode, string message = null, object data = null);
    }
}
