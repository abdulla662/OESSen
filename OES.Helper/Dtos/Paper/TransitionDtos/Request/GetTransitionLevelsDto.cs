

namespace OES.Helper.Dtos.Paper.TransitionDtos.Request
{
    public class GetTransitionLevelsDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public decimal LowerDScore { get; set; }

        public decimal UpperDScore { get; set; }

        public long TransitionProfileId { get; set; }

        public long DifficultyLevelId { get; set; }

        public long QuestionCategoryId { get; set; }

        public string QuestionCategoryName { get; set; }

        public string LowerDScoreFormatted => LowerDScore.ToString("F2");

        public string UpperDScoreFormatted => UpperDScore.ToString("F2");

        public decimal LowerDScoreRounded => Math.Round(LowerDScore, 2);

        public decimal UpperDScoreRounded => Math.Round(UpperDScore, 2);
    }
}
