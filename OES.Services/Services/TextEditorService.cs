using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.TextEditor;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;

namespace OES.Services.Services
{
    public class TextEditorService : ITextEditorService
    {
        public async Task<EditorFileUploadResponseDto> UploadImageAsync(HttpRequest request, string webRootPath)
        {
            var response = new EditorFileUploadResponseDto();

            // Validate the request
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                response.ErrorMessage = Resource.Noimagefilesuploaded;
                return response;
            }

            var formFile = request.Form.Files[0];
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };

            // Validate file extension
            var fileExtension = Path.GetExtension(formFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                response.ErrorMessage = Resource.Invalidimageformat + formFile.FileName;
                return response;
            }

            // Validate the web root path
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                response.ErrorMessage = Resource.Emptyornullwebrootpath;
                return response;
            }

            // Create the images directory if it doesn't exist
            var uploadPath = Path.Combine(webRootPath, "images");
            Directory.CreateDirectory(uploadPath);

            // Generate unique filename and save file
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            // Prepare the response
            var relativeImageUrl = $"images/{fileName}";
            string absoluteImageUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/{relativeImageUrl}";
            response.Result = [
                new EditorFileUploadResultDto
                {
                    Url = absoluteImageUrl,
                    Name = formFile.FileName,
                    Size = formFile.Length
                }
            ];

            return response;
        }

        public async Task<EditorFileUploadResponseDto> UploadVideoAsync(HttpRequest request, string webRootPath)
        {
            var response = new EditorFileUploadResponseDto();

            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                response.ErrorMessage = Resource.Novideofilesuploaded;
                return response;
            }

            var formFile = request.Form.Files[0];
            var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv" };

            // Validate file extension
            var fileExtension = Path.GetExtension(formFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                response.ErrorMessage = $"Invalid video format: {formFile.FileName}";
                return response;
            }

            // Validate the web root path
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                response.ErrorMessage = "Empty or null web root path.";
                return response;
            }

            // Create the videos directory if it doesn't exist
            var uploadPath = Path.Combine(webRootPath, "videos");
            Directory.CreateDirectory(uploadPath);

            // Generate unique filename and save file
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            // Prepare the response
            var relativeVideoUrl = $"videos/{fileName}";
            string absoluteVideoUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/{relativeVideoUrl}";
            response.Result = [
                new EditorFileUploadResultDto
                {
                    Url = absoluteVideoUrl,
                    Name = formFile.FileName,
                    Size = formFile.Length
                }
            ];

            return response;
        }

        public async Task<EditorFileUploadResponseDto> UploadAudioAsync(HttpRequest request, string webRootPath)
        {
            var response = new EditorFileUploadResponseDto();

            if (!request.HasFormContentType || request.Form.Files.Count == 0)
            {
                response.ErrorMessage = "No audio files uploaded.";
                return response;
            }

            var formFile = request.Form.Files[0];
            var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a" };

            // Validate file extension
            var fileExtension = Path.GetExtension(formFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                response.ErrorMessage = $"Invalid audio format: {formFile.FileName}";
                return response;
            }

            // Validate the web root path
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                response.ErrorMessage = "Empty or null web root path.";
                return response;
            }

            // Create the audios directory if it doesn't exist
            var uploadPath = Path.Combine(webRootPath, "audios");
            Directory.CreateDirectory(uploadPath);

            // Generate unique filename and save file
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadPath, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            // Prepare the response
            var relativeAudioUrl = $"audios/{fileName}";
            string absoluteAudioUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/{relativeAudioUrl}";
            response.Result = [
                new EditorFileUploadResultDto
                {
                    Url = absoluteAudioUrl,
                    Name = formFile.FileName,
                    Size = formFile.Length
                }
            ];

            return response;
        }

        public EditorFileDeletionResponseDto DeleteFile(string directory, string fileName, string webRootPath)
        {
            var response = new EditorFileDeletionResponseDto();

            if (string.IsNullOrEmpty(fileName))
            {
                response.ErrorMessage = "File name is required.";
                return response;
            }

            var filePath = Path.Combine(webRootPath, directory, fileName);

            if (!File.Exists(filePath))
            {
                response.ErrorMessage = $"The file '{fileName}' does not exist in the '{directory}' directory.";
                return response;
            }

            try
            {
                File.Delete(filePath);
                response.SuccessMessage = $"The file '{fileName}' was successfully deleted from the '{directory}' directory.";
                return response;
            }
            catch (Exception ex)
            {
                response.ErrorMessage = $"An error occurred while deleting the file: {ex.Message}";
                return response;
            }
        }
    }
}
