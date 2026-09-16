namespace OES.Core.Entities
{
    public class CTRResultsFileName : BaseEntity<long>
    {
        public string FileName { get; set; }

        public DateTime SentAt { get; set; }

        public string SentBy { get; set; }
    }
}
