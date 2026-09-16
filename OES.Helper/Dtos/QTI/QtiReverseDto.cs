namespace OES.Helper.Dtos.QTI
{
    public class QtiReverseDto
    {
        public long AssessmentIdentifier { get; set; }
        public string AssessmentCode { get; set; }
        public string QuestionCode { get; set; }
        public List<string> AssessmentCorrectResponse { get; set; }
        public string AssessmentBody { get; set; }
        public long MaxChoices { get; set; }
        public string AssessmentQuestionType { get; set; }
        public List<AssessmentChoices> AssessmentChoices { get; set; } = [];
    }
}
