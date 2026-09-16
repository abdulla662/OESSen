namespace OES.Helper.Dtos.SectionDistributionDto.Common
{
    public class SectionDistributionDto
    {
        public long ItemBankId { get; set; }

        public long QuestionTypeID { get; set; }

        public long DifficultyLevelID { get; set; }

        public long SelectedCount { get; set; }

        public long SubQuestionCount { get; set; }

        public List<long> QuestionIds { get; set; } = [];
    }
}
