namespace OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos
{
    public class PaginatedListSubQuestionDto
    {
        public long QuestionMetadataId { get; set; }

        public string Body { get; set; }

        public List<int> ChoicesCountsList { get; set; }

        public int NumberOfLanguages { get; set; }

        public List<long> LanguagesIds { get; set; } = [];

        public string LanguageNames { get; set; }

        public string Delta { get; set; }
    }
}
