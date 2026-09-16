using Microsoft.AspNetCore.Mvc;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class EditorsController : OESBaseController
    {
        private readonly string _webRootPath;

        private readonly ITextEditorService _textEditorService;

        public EditorsController(IWebHostEnvironment webHostEnvironment, ITextEditorService textEditorService)
        {
            _webRootPath = webHostEnvironment.WebRootPath;
            _textEditorService = textEditorService;
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImageAsync()
        {
            var response = await _textEditorService.UploadImageAsync(Request, _webRootPath);

            return string.IsNullOrWhiteSpace(response.ErrorMessage) ? Ok(response) : BadRequest(response);
        }


        [HttpPost("upload-video")]
        public async Task<IActionResult> UploadVideoAsync()
        {
            var response = await _textEditorService.UploadVideoAsync(Request, _webRootPath);

            return string.IsNullOrWhiteSpace(response.ErrorMessage) ? Ok(response) : BadRequest(response);
        }


        [HttpPost("upload-audio")]
        public async Task<IActionResult> UploadAudioAsync()
        {
            var response = await _textEditorService.UploadAudioAsync(Request, _webRootPath);

            return string.IsNullOrWhiteSpace(response.ErrorMessage) ? Ok(response) : BadRequest(response);
        }


        [HttpDelete("delete-image")]
        public IActionResult DeleteImage(string fileName)
        {
            var response = _textEditorService.DeleteFile("images", fileName, _webRootPath);

            return string.IsNullOrWhiteSpace(response.ErrorMessage) ? Ok(response) : BadRequest(response);
        }


        [HttpDelete("delete-video")]
        public IActionResult DeleteVideo(string fileName)
        {
            var response = _textEditorService.DeleteFile("videos", fileName, _webRootPath);

            return string.IsNullOrWhiteSpace(response.ErrorMessage) ? Ok(response) : BadRequest(response);
        }


        [HttpDelete("delete-audio")]
        public IActionResult DeleteAudio(string fileName)
        {
            var response = _textEditorService.DeleteFile("audios", fileName, _webRootPath);

            return string.IsNullOrWhiteSpace(response.ErrorMessage) ? Ok(response) : BadRequest(response);
        }
    }
}
