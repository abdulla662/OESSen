using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.ExportFiles;
using OES.Helper.General;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QTIController(IQTISerializeService _qtiSerializer) : OESBaseController
    {
        [HttpPost("ExportQuestionsAsQtiFile")]
        [OESFilter(Authorize = true)]
        [DoNotEncrypt]
        public async Task<IActionResult> ExportQuestionsAsQtiFileAsync(ExportDataFileDto exportDataFileDto)
        {
            var byteArray = await _qtiSerializer.ExportQuestionsAsQtiFileAsync(exportDataFileDto);

            const string contentType = "application/txt";

            const string fileName = "ExportedQuestions.txt";

            return File(byteArray, contentType, fileName);
        }
    }
}
