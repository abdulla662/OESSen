namespace OES.Helper.Dtos.Section
{
    public class AutoSectionRawInternal
    {
        public long PaperId { get; set; }
        public long? SectionId { get; set; }
        public long QuestionTypeId { get; set; }
        public long DifficultyLevelId { get; set; }
        public long SelectedCount { get; set; }
        public string QuestionIdsString { get; set; }
    }
}
