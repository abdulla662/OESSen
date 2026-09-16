namespace OES.Helper.QTICrypto
{
    public class QTIBase
    {
        /* QTI TAGS */

        // This is for single and multiple choices question types:
        public const string QtiAssessmentItem = "qti-assessment-item";
        public const string QtiResponseDeclaration = "qti-response-declaration";
        public const string QtiCorrectResponse = "qti-correct-response";
        public const string QtiValue = "qti-value";
        public const string QtiItemBody = "qti-item-body";
        public const string QtiChoiceInteraction = "qti-choice-interaction";
        public const string QtiSimpleChoice = "qti-simple-choice";
        public const string QtiOutcomeDeclaration = "qti-outcome-declaration";
        public const string QtiDefaultValue = "qti-default-value";
        public const string QtiPrompt = "qti-prompt";
        public const string QtiAssessment = "qti-assessment";

        // This is for essay question type:
        public const string QtiExtendedTextInteraction = "qti-extended-text-interaction";

        // This is for text completion:
        public const string QtiMapping = "qti-mapping";
        public const string QtiMapEntry = "qti-map-entry";
        public const string QtiTextEntryInteraction = "qti-text-entry-interaction";

        // Attributes:
        public const string Title = "title";
        public const string Code = "Code";
        public const string BaseType = "base-type";
        public const string Cardinality = "cardinality";
        public const string Identifier = "identifier";
        public const string MaxChoices = "max-choices";
        public const string ResponseIdentifier = "response-identifier";
        public const string DefaultValue = "default-value";
        public const string MapKey = "map-key";
        public const string MappedValue = "mapped-value";
        public const string CaseSensitive = "case-sensitive";

        // Attributes values:
        public const string Single = "single";
        public const string Multiple = "multiple";
        public const string Response = "RESPONSE";
        public const string Score = "SCORE";
        public const string Float = "float";
        public const string True = "true";
        public const string False = "false";

        // Common values:
        public const string Paragraph = "p";
        public const string Blockquote = "blockquote";
        public const string Comment = "New Question";
    }
}
