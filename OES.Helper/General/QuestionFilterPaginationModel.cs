
namespace OES.Helper.General
{
    public class QuestionFilterPaginationModel
    {
        public long _selectedItemBank { get; set; }
        public string _selectedItemBankSignature { get; set; }
        public long _selectedType { get; set; }
        public QuestionStatus _selectedStatus { get; set; }
        public long _selectedCategory { get; set; }
        public long _selectedDeltaType { get; set; }
        public long _selectedDifficultyLevel { get; set; }
        public long _selectedDifficultyProfile { get; set; }
        public decimal _fromDelta { get; set; }
        public decimal _toDelta { get; set; }
        public long _selectedLanguage { get; set; }
        public long _selectedBranchId { get; set; }
        public bool _isConsiderDifficulty { get; set; } = true;
    }
}
