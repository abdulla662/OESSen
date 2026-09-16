using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Question.WebSearchQuestionDtos
{
    public class WebSearchResultDto
    {
        public WebSearchResultDto(
            string title,
            string url,
            string description,
            bool isCorrect,
            int sortOrder,
            bool isSystemGenerated
        )
        {
            Title = title;
            Url = url;
            Description = description;
            IsCorrect = isCorrect;
            SortOrder = sortOrder;
            IsSystemGenerated = isSystemGenerated;
        }

        public WebSearchResultDto() { }

        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = nameof(Resource.UrlRequired), ErrorMessageResourceType = typeof(Resource))]
        [RegularExpression(RegularExpressions.RegularExpressions.UrlCheck, ErrorMessageResourceName = nameof(Resource.UrlInvalid), ErrorMessageResourceType = typeof(Resource))]
        public string Url { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        public int SortOrder { get; set; }

        public bool IsSystemGenerated { get; set; }
    }
}