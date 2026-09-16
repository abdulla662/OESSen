namespace OES.Helper.Dtos.Question.WebSearchQuestionDtos
{
    public class WebSearchPropertiesDto
    {
        public List<string> Keywords { get; set; } = [];

        public List<WebSearchResultDto> Results { get; set; } = [];
    }
}