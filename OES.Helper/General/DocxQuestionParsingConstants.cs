namespace OES.Helper.General
{
    public static class DocxQuestionParsingConstants
    {

        // Split / Detection Patterns
        public const string QuestionCodeSplitPattern = @"(?=\[\[\s*Question\s*Code)";
        public const string QuestionCodeKeywordPattern = @"Question\s*Code";
        public const string QuestionCodeStartPattern = @"\[\[\s*Question\s*Code\s*:";

        // Field Extraction Patterns
        public const string QuestionCodePattern = @"\[\[\s*Question\s*Code\s*:\s*([^\]\n\r]+)";
        public const string ItemBankCodePattern = @"\[\[\s*Item\s*Bank\s*Code\s*:\s*([^\]\n\r]+)";
        public const string ModelAnswerPattern = @"\[\[\s*Model\s*Answer\s*:\s*(.*?)(?:\]\]|$)";
        public const string ChoicesTagPattern = @"\[\[\s*Choices";
        public const string ChoicesPrefixPattern = @"\[\[\s*Choices\s*:?\s*";
        public const string ModelAnswerTagPattern = @"\[\[\s*Model\s*Answer";

        // Body Section Boundaries
        public const string QuestionBodyStartPattern = @"\[\[\s*Question\s*:\s*";
        public const string QuestionBodyEndPattern = @"\[\[\s*Choices|\[\[\s*Model\s*Answer";

        // Choice Text Cleaning
        public const string ChoiceItemPattern = @"([a-zA-Z0-9])\)\s*([^a-zA-Z0-9\)]+)";
        public const string BulletPrefixPattern = @"^[•\-\*\s]+";
        public const string ChoiceLetterPrefixPattern = @"^[a-zA-Z0-9][\)\.]\s*";

        // Body Text Cleaning
        public const string TrailingBracketsPattern = @"\]\]\s*$";
        public const string TrailingDotsBracketsPattern = @"[\.\[]+$";
        public const string MultipleDotsPattern = @"\.{3,}";

        // Tokens
        public const string DoubleClosingBracket = "]]";
        public const string SingleClosingBracket = "]";
        public const string CorrectAnswerMarker = "*";

        // HTML / Image
        public const string ImgCssClass = "img-fluid d-block my-2";
        public const int ChoiceImageSize = 200;
        public const int BodyImageSize = 300;
    }
}
