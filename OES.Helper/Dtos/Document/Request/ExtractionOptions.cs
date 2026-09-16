using OES.Helper.General;

namespace OES.Helper.Dtos.Document.Request
{
    public sealed record ExtractionOptions
    {
        public bool EnableOcr { get; init; } = true;

        public string OcrLanguage { get; init; } = MiscConstants.DefaultOcrLanguage;

        public bool UseDocling { get; init; } = false;
    }
}
