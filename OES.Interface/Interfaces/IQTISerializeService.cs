using OES.Helper.Dtos.ExportFiles;

namespace OES.Interface.Interfaces
{
    public interface IQTISerializeService
    {
        Task<byte[]> ExportQuestionsAsQtiFileAsync(ExportDataFileDto exportDataFileDto);
    }
}
