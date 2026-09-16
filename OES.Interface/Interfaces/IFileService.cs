using Microsoft.AspNetCore.Http;
using OES.Helper.Dtos.ExportFiles;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IFileService
    {
        Task<IApiResponse> HandleFileUploadAsync(IFormFile formFile, string questionTypeNames);
        Task<List<UploadQuestionDetailsDto>> ParseQuestionsFromFile(string filePath);
        Task<List<UploadQuestionDetailsDto>> ExtractQuestionsFromDocxToDtos(string filePath);
        Task<List<UploadQuestionDetailsDto>> ExtractQuestionsFromPdfToDtos(string filePath);
        Task<List<UploadQuestionDetailsDto>> ExtractQuestionsFromExcel(string filePath);
        Task<IApiResponse> AddFileQuestions(string uploadQuestions, string metadata);
        Task<IApiResponse> ExportQuestionDetailsToFileAsync(List<long> questionIds, long typeId, long languageId);
        Task<IApiResponse> ExportQuestionDetailsToFileAsync(List<long> questionIds, long languageId);
        Task<byte[]> ExportQuestionItemBankHierarchyToFileAsync(ExportDataFileDto exportDataFileDto);
        Task<IApiResponse> ExportQuestionDetailsToDocxFileAsync(List<long> questionIds, long typeId, long languageId);
        Task<IApiResponse> ExportQuestionDetailsToDocxFileAsync(List<long> questionIds, long languageId);
        Task<byte[]> ExportQuestionItemBankHierarchyToDocxFileAsync(ExportDataFileDto exportDataFileDto);
        Task<ApiResponse> UploadCandidatesExcelFileAsync(IFormFile excelFile, string fileName, CancellationToken cancellationToken = default);
    }
}
