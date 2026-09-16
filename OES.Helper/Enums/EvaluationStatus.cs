namespace OES.Helper.Enums
{
    public enum EvaluationStatus
    {
        NotRequired = 0,            // Means the evaluation process doesn't need answer because it is auto-correct.
        Required = 1,               // Means the evaluation process needs answer because it requires manual evaluation.
        PendingEvaluation = 2,      // Means the evaluation process has a pending answer as it should be sent to evaluation system to be evaluated.
        EvaluationCompleted = 3     // Means the evaluation process is completed.
    }
}
