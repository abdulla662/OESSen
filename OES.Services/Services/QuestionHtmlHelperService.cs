using OES.Core.Entities;
using OES.Helper.Dtos.FileDetails;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Text.RegularExpressions;

namespace OES.Services.Services
{
    public class QuestionHtmlHelperService : IQuestionHtmlHelperService
    {
        private static Regex CreateDocumentUrlRegex()
        {
            var baseUrl = CentralizedUrlHelper.DocLibApiBaseUrl.TrimEnd('/');

            var escapedBaseUrl = Regex.Escape(baseUrl);

            return new Regex(
                $@"({escapedBaseUrl}\/api\/Document\/DownloadStream\?documentId=([a-fA-F0-9\-]{{36}}))",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );
        }

        public List<FileUrlWithFileId> ExtractDocumentUrlsAndIds(string htmlContent)
        {
            var matches = CreateDocumentUrlRegex().Matches(htmlContent);

            var result = new List<FileUrlWithFileId>();

            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count > 2)
                {
                    result.Add(new FileUrlWithFileId
                    {
                        Url = match.Groups[1].Value,
                        FileId = match.Groups[2].Value
                    });
                }
            }

            return result;
        }

        public List<QuestionDocLibFile> ExtractMediaEntitiesFromQuestionDetails(IEnumerable<QuestionDetails> questionDetailsList)
        {
            var allMediaEntities = new List<QuestionDocLibFile>();

            foreach (var detail in questionDetailsList)
            {
                var extractedFiles = new List<FileUrlWithFileId>();

                if (!string.IsNullOrWhiteSpace(detail.Body))
                    extractedFiles.AddRange(ExtractDocumentUrlsAndIds(detail.Body));

                if (!string.IsNullOrWhiteSpace(detail.ModelAnswer))
                    extractedFiles.AddRange(ExtractDocumentUrlsAndIds(detail.ModelAnswer));

                if (!string.IsNullOrWhiteSpace(detail.Instructions))
                    extractedFiles.AddRange(ExtractDocumentUrlsAndIds(detail.Instructions));

                if (detail.QuestionsChoices?.Any() == true)
                {
                    foreach (var choice in detail.QuestionsChoices)
                    {
                        if (!string.IsNullOrWhiteSpace(choice.ChoiceText))
                        {
                            extractedFiles.AddRange(ExtractDocumentUrlsAndIds(choice.ChoiceText));
                        }
                    }
                }

                var mediaEntities = extractedFiles
                    .Where(x => Guid.TryParse(x.FileId, out _))
                    .Select(x => new QuestionDocLibFile
                    {
                        QuestionId = detail.QuestionMetadataId,
                        FileURL = x.Url,
                        FileId = Guid.Parse(x.FileId)
                    });

                allMediaEntities.AddRange(mediaEntities);
            }

            return [.. allMediaEntities.DistinctBy(x => x.FileId)];
        }
    }
}
