namespace OES.Helper.Dtos.Question
{
    public class QuestionDistributionRequirementDto
    {
        public long QuestionTypeID { get; set; }

        public long DifficultyLevelID { get; set; }

        public long RequiredCount { get; set; }

        public long SubQuestionCount { get; set; }
    }
}
