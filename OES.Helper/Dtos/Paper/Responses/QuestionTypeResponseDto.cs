namespace OES.Helper.Dtos.Paper.Responses
{
    public class QuestionTypeResponseDto
    {
        public long QuestionTypeId { get; set; }

        public string QuestionTypeName { get; set; }

        public long NumberOfQuestions { get; set; } // Related to question type in difficulty level
    }
}
