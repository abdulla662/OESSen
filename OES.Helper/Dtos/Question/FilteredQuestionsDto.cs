using System.ComponentModel;

namespace OES.Helper.Dtos.Question
{
    public class FilteredQuestionsDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Category { get; set; }
        public string QuestionTypeName { get; set; }
        public DateTime? CreatedDate { get; set; }
        [DisplayName("CreatedDate")]
        public string CreatedDateDisplay => CreatedDate?.ToString("dd/MM/yyyy HH:mm");
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        [DisplayName("ModifiedDate")]
        public string ModifiedDateDisplay => ModifiedDate?.ToString("dd/MM/yyyy HH:mm");
        public string ModifiedBy { get; set; }
    }
}
