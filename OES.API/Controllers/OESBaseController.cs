using Microsoft.AspNetCore.Mvc;
using OES.Helper.General;

namespace OES.API.Controllers
{
    /// <summary>
    /// API Base Controller.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [EncryptedPayload]
    public class OESBaseController : ControllerBase { }
}
