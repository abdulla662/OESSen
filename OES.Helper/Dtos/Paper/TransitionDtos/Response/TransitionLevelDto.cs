namespace OES.Helper.Dtos.Paper.TransitionDtos.Response
{
    public class TransitionLevelDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public decimal LowerDScore { get; set; }

        public decimal UpperDScore { get; set; }

        public string TransitionProfileName { get; set; }

        public string TransitionProfileDescription { get; set; }

        public string LowerDScoreFormatted => LowerDScore.ToString("F9");

        public string UpperDScoreFormatted => UpperDScore.ToString("F9");

        public decimal LowerDScoreRounded => Math.Round(LowerDScore, 2);

        public decimal UpperDScoreRounded => Math.Round(UpperDScore, 2);
    }
}
