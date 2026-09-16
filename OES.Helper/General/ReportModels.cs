namespace OES.Helper.General
{
    public class CandidateSessionData
    {
        public long CandidateId { get; set; }
        public int Gender { get; set; }
        public double RawScore { get; set; }
        public double ScaledScore { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class ItemResponseData
    {
        public long CandidateId { get; set; }
        public string QuestionCode { get; set; }
        public string ChosenOption { get; set; }
        public bool IsCorrect { get; set; }
        public TimeSpan TimeSpent { get; set; }
    }

    public class ItemInfoData
    {
        public string QuestionCode { get; set; }
        public string Status { get; set; }
        public string CorrectAnswerKey { get; set; }
    }

    public class RawReportData
    {
        public List<CandidateSessionData> Sessions { get; set; } = [];
        public List<ItemResponseData> Responses { get; set; } = [];
        public List<ItemInfoData> Items { get; set; } = [];
        public string ClientName { get; set; }
        public string ExamCode { get; set; }
        public string ExamTitle { get; set; }
        public string FormName { get; set; }
    }

    public class ExamFormRow
    {
        public string ClientName { get; set; }
        public string ExamTitle { get; set; }
        public string Exam { get; set; }
        public string Form { get; set; }
        public int OpItems { get; set; }
        public int N { get; set; }
        public double RawScoreMax { get; set; }
        public double RawScoreMean { get; set; }
        public double RawScoreMedian { get; set; }
        public double RawScoreMin { get; set; }
        public double RawScoreP25 { get; set; }
        public double RawScoreP75 { get; set; }
        public double RawScoreP95 { get; set; }
        public double RawScoreStdev { get; set; }
        public double ScaledScoreMax { get; set; }
        public double ScaledScoreMean { get; set; }
        public double ScaledScoreMedian { get; set; }
        public double ScaledScoreMin { get; set; }
        public double ScaledScoreP25 { get; set; }
        public double ScaledScoreP75 { get; set; }
        public double ScaledScoreP95 { get; set; }
        public double ScaledScoreStdev { get; set; }
        public TimeSpan TestTimeMax { get; set; }
        public TimeSpan TestTimeMean { get; set; }
        public TimeSpan TestTimeMedian { get; set; }
        public TimeSpan TestTimeMin { get; set; }
        public TimeSpan TestTimeP25 { get; set; }
        public TimeSpan TestTimeP75 { get; set; }
        public TimeSpan TestTimeP95 { get; set; }
        public TimeSpan TestTimeStdev { get; set; }
        public double CronbachAlpha { get; set; }
        public double StandardErrorMeasurement { get; set; }
        public double MeanPValue { get; set; }
        public double MeanItemTotalCorrelation { get; set; }
    }

    public class ItemRow
    {
        public string ClientName { get; set; }
        public string Exam { get; set; }
        public string Form { get; set; }
        public string Item { get; set; }
        public string Status { get; set; }
        public string Key { get; set; }
        public int N { get; set; }
        public double PValue { get; set; }
        public double PValueVariance { get; set; }
        public double? ItemTotalCorrelation { get; set; }
        public TimeSpan MeanResponseTime { get; set; }
        public TimeSpan StDevResponseTime { get; set; }
        public TimeSpan MinResponseTime { get; set; }
        public TimeSpan MedianResponseTime { get; set; }
        public TimeSpan MaxResponseTime { get; set; }
    }

    public class ResponseRow
    {
        public string ClientName { get; set; }
        public string Exam { get; set; }
        public string Form { get; set; }
        public string QuestionCode { get; set; }
        public string Status { get; set; }
        public string Key { get; set; }
        public double MetricA { get; set; }
        public double MetricB { get; set; }
        public double MetricC { get; set; }
        public double MetricD { get; set; }
        public double MetricZBlank { get; set; }
        public double? OptionTotalCorrelationA { get; set; }
        public double? OptionTotalCorrelationB { get; set; }
        public double? OptionTotalCorrelationC { get; set; }
        public double? OptionTotalCorrelationD { get; set; }
        public double? OptionTotalCorrelationZBlank { get; set; }
    }
}
