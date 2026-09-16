using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.ExportFiles;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.Services;
using System.Net;

namespace OES.API.Controllers
{
    public class UploadController(IFileService _fileService) : OESBaseController
    {
        [HttpPost("UploadFile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UploadQuestions(IFormFile formFile, [FromForm] string questionTypeNames)
        {
            var (isValid, error) = await DocLibFileUploadValidationService.ValidateAsync(formFile);

            if (!isValid)
            {
                return new ApiResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = error ?? Resource.InvalidFileFormat
                };
            }

            return await _fileService.HandleFileUploadAsync(formFile, questionTypeNames);
        }

        [HttpPost("AddFile")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddFileQuestions([FromForm] string uploadQuestions, [FromForm] string metadata)
        {
            return await _fileService.AddFileQuestions(uploadQuestions, metadata);
        }

        /// <summary>
        /// Export question details to an Excel file based on the specified criteria.
        /// </summary>
        /// <param name="request">The export request containing the question IDs, type ID, and language ID.</param>
        /// <returns>Returns the API response with the status of the export operation.</returns>
        [HttpPost("ExportQuestion")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> ExportQuestionDetails(ExportQuestionDto request)
        {
            return await _fileService.ExportQuestionDetailsToFileAsync(request.QuestionIds, request.TypeId, request.LanguageId);
        }

        /// <summary>
        /// Export question details to an Excel file based on the specified question IDs and language ID.
        /// </summary>
        /// <param name="request">The export request containing the question IDs and language ID.</param>
        /// <returns>Returns the API response with the status of the export operation.</returns>
        [HttpPost("ExportQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> ExportQuestionDetails(ExportQuestionsDto request)
        {
            return await _fileService.ExportQuestionDetailsToFileAsync(request.QuestionIds, request.LanguageId);
        }

        /// <summary>
        /// Export question details from the specified item bank to an Excel file based on the given item bank ID and language ID.
        /// </summary>
        /// <returns>Returns the API response with the status of the export operation.</returns>
        [HttpPost("ExportQuestionItemBankToXslx")]
        [OESFilter(Authorize = true)]
        [DoNotEncrypt]
        public async Task<IActionResult> ExportQuestionInItemBank(ExportDataFileDto exportDataFileDto)
        {
            var fileBytes = await _fileService.ExportQuestionItemBankHierarchyToFileAsync(exportDataFileDto);

            return File(fileBytes, MiscConstants.ExcelContentType, MiscConstants.ExportedQuestionsFileName);
        }

        /// <summary>
        /// Export question details to a DOCX file based on the specified criteria.
        /// </summary>
        /// <param name="request">The export request containing the question IDs, type ID, and language ID.</param>
        /// <returns>Returns the API response with the status of the export operation.</returns>
        [HttpPost("ExportQuestionDetailsToDocx")]
        [OESFilter(Authorize = true)]
        [DoNotEncrypt]
        public async Task<IApiResponse> ExportQuestionDetailsToDocxFileAsync(ExportQuestionDto request)
        {
            return await _fileService.ExportQuestionDetailsToDocxFileAsync(request.QuestionIds, request.TypeId, request.LanguageId);
        }

        /// <summary>
        /// Export question details to a DOCX file based on the specified question IDs and language ID.
        /// </summary>
        /// <param name="request">The export request containing the question IDs and language ID.</param>
        /// <returns>Returns the API response with the status of the export operation.</returns>
        [HttpPost("ExportQuestionsDetailsToDocx")]
        [OESFilter(Authorize = true)]
        [DoNotEncrypt]
        public async Task<IApiResponse> ExportQuestionDetailsToDocxFileAsync(ExportQuestionsDto request)
        {
            return await _fileService.ExportQuestionDetailsToDocxFileAsync(request.QuestionIds, request.LanguageId);
        }

        /// <summary>
        /// Export question details from the specified item bank to an Docx file based on the given item bank ID and language ID.
        /// </summary>
        /// <param name="exportDataFileDto">The export request containing the item banks IDs and language ID.</param>
        /// <returns>Returns the API response with the status of the export operation.</returns>
        [HttpPost("ExportQuestionItemBankToDocx")]
        [OESFilter(Authorize = true)]
        [DoNotEncrypt]
        public async Task<IActionResult> ExportQuestionDetailsToDocx(ExportDataFileDto exportDataFileDto)
        {
            var fileBytes = await _fileService.ExportQuestionItemBankHierarchyToDocxFileAsync(exportDataFileDto);

            const string contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            const string fileName = "ExportedQuestions.docx";

            return File(fileBytes, contentType, fileName);
        }
    }
}
