using OES.Helper.Dtos.FormQuestions;

namespace OES.Helper.Dtos.Paper.Responses
{
    public class GetPaperWithFormsDto
    {
        public long PaperId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public string LanguageDirection { get; set; }

        public string Abbreviation { get; set; }

        public int QuestionsCount { get; set; }

        public float Duration { get; set; }

        public long TotalMarks { get; set; }

        public string Type { get; set; }

        public string Language { get; set; } = string.Empty;

        public string DifficultyProfile { get; set; } = string.Empty;

        public List<FormQuestionsDto> Forms { get; set; } = [];
    }
}
