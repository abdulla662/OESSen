using OES.Core.Entities;
using OES.Helper.Dtos.FileDetails;

namespace OES.Interface.Interfaces
{
    public interface IQuestionHtmlHelperService
    {
        List<FileUrlWithFileId> ExtractDocumentUrlsAndIds(string htmlContent);

        List<QuestionDocLibFile> ExtractMediaEntitiesFromQuestionDetails(IEnumerable<QuestionDetails> questionDetailsList);
    }
}
