
namespace OES.Helper.Dtos.Paper.Responses
{
    public class GetDifficultyLevelMarkingSchemeResponseDto
    {
        public long DifficultyLevelId { get; set; }
        public string DifficultyLevelName { get; set; }
        public long SelectedCount { get; set; }
        public long SubQuestionCount { get; set; }
        public long TotalCount { get; set; }
        public double MarkPerQuestion { get; set; }
        public long? FormId { get; set; } // NOTE: This field may be nullable or zero in case of 'Auto' paper.
    }
}
