namespace OES.Helper.Dtos.ExamServer
{
    public class QuestionChoiceDto
    {
        public long Id { get; set; }
        public string ChoiceText { get; set; }
        public bool IsCorrectAnswer { get; set; }
        public string? AttachmentFileName { get; set; }
        public int ChoiceOrderId { get; set; }
    }
}