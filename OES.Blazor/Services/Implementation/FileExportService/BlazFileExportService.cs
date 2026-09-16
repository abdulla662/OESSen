using Microsoft.JSInterop;
using OES.Blazor.Services.Interfaces.FileExportService;
using OES.Helper.Dtos.UploadFiles;
using SharedHelper.General;

namespace OES.Blazor.Services.Implementation.FileExportService
{
    public class BlazFileExportService : IBlazFileExportService
    {
        private readonly IJSRuntime _jSRuntime;

        public BlazFileExportService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task ExportItemBankQuestionsAsExcelFileAsync(ExportQuestionInItemBankDto exportQuestionInItemBankDto)
        {
            var url = $"{CentralizedUrlHelper.OesApiBaseUrl}api/Upload/ExportQuestionItemBankToXslx";

            const string fileName = "ExportedQuestions.xlsx";

            await _jSRuntime.InvokeVoidAsync("downloadFileUsingFetch", url, fileName, exportQuestionInItemBankDto);
        }

        public async Task ExportItemBankQuestionsAsDocxFileAsync(ExportQuestionInItemBankDto exportQuestionInItemBankDto)
        {
            var url = $"{CentralizedUrlHelper.OesApiBaseUrl}api/Upload/ExportQuestionItemBankToDocx";

            const string fileName = "ExportedQuestions.docx";

            await _jSRuntime.InvokeVoidAsync("downloadFileUsingFetch", url, fileName, exportQuestionInItemBankDto);
        }

        public async Task ExportItemBankQuestionsAsQtiXmlFileAsync(ExportQuestionInItemBankDto exportQuestionInItemBankDto)
        {
            var url = $"{CentralizedUrlHelper.OesApiBaseUrl}api/QTI/ExportQuestionsAsQtiFile";

            const string fileName = "ExportedQuestions.xml";

            await _jSRuntime.InvokeVoidAsync("downloadFileUsingFetch", url, fileName, exportQuestionInItemBankDto);
        }
    }
}
