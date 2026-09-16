using OES.Helper.Dtos.UploadFiles;

namespace OES.Blazor.Services.Interfaces.FileExportService
{
    public interface IBlazFileExportService
    {
        Task ExportItemBankQuestionsAsExcelFileAsync(ExportQuestionInItemBankDto exportQuestionInItemBankDto);

        Task ExportItemBankQuestionsAsDocxFileAsync(ExportQuestionInItemBankDto exportQuestionInItemBankDto);

        Task ExportItemBankQuestionsAsQtiXmlFileAsync(ExportQuestionInItemBankDto exportQuestionInItemBankDto);
    }
}
