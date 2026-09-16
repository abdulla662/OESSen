using OES.Helper.Enums;

namespace OES.Helper.Dtos.Question.QuestionDetailsDtos
{
    public class PendingQuestionPaginationDto
    {
        public long Id { get; set; }

        public string Code { get; set; }

        public string Subject { get; set; }

        public string ItemBank { get; set; }

        public long ItemBankId { get; set; }

        public string Type { get; set; }

        public string TypeDisplay => Type?.ToLocalizedString<QuestionType>(); // Note: This method is referenced using reflection, don't delete it, as it has 0 references.

        public string Category { get; set; }

        public string Language { get; set; }

        public QuestionStatus Status { get; set; }

        public string StatusDisplay => Status.ToLocalizedString();

        public override bool Equals(object obj)
        {
            return obj is PendingQuestionPaginationDto other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
