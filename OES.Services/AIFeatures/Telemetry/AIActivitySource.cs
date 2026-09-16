using System.Diagnostics;

namespace OES.Services.AIFeatures.Telemetry
{
    public static class AIActivitySource
    {
        public const string SourceName = "OES.AI";
        public static readonly ActivitySource Source = new(SourceName);

        public static Activity? StartQuestionGeneration(long itemBankId) =>
            Source.StartActivity("QuestionGeneration")
                ?.SetTag("ai.itembank.id", itemBankId);

        public static Activity? StartItemBankGeneration(string levelsCreationStrategy) =>
            Source.StartActivity("ItemBankGeneration")
                ?.SetTag("ai.itembank.levelsStrategy", levelsCreationStrategy);

        public static Activity? StartDocumentExtraction(string fileName, string documentType) =>
            Source.StartActivity("DocumentExtraction")
                ?.SetTag("ai.document.filename", fileName)
                ?.SetTag("ai.document.type", documentType);

        public static Activity? StartStructuredOutputGeneration(string targetType) =>
            Source.StartActivity("StructuredOutputGeneration")
                ?.SetTag("ai.output.type", targetType);
    }
}
