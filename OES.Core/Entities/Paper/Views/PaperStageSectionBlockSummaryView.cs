namespace OES.Core.Entities.Paper.Views
{
    public class PaperStageSectionBlockSummaryView
    {
        public long PaperId { get; set; }

        public string PaperName { get; set; }

        public long StageId { get; set; }

        public string StageName { get; set; }

        public int StageOrder { get; set; }

        public decimal StageTime { get; set; }

        public long SectionId { get; set; }

        public string SectionName { get; set; }

        public int SectionOrder { get; set; }

        public long? BlockId { get; set; }

        public string BlockName { get; set; }

        public string BlockCode { get; set; }

        public int QuestionCount { get; set; }
    }
}
