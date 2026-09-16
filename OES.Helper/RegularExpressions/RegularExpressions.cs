namespace OES.Helper.RegularExpressions
{
    public static class RegularExpressions
    {
        public const string EmailExpression = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const string ComplexPassword = "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[@$!%*?&])[A-Za-z\\d@$!%*?&]{8,}$";
        public const string FirstName = "^[a-zA-Z]+$";
        public const string LastName = "^[a-zA-Z]+$";
        public const string PhoneNumber = @"^(012|011|010|015)\d{8}$"; // Egyptian Phone Number
        public const string GeneralMobileNumber = @"^\+?[0-9]{8,15}$"; // More General Phone Number
        public const string InternationalPhoneNumber = @"(?:(?:(?:\+|00)20|0)1[0125](?:[ .-]?\d){8}|(?:(?:\+|00)966|0)5(?:[ .-]?\d){8}|966[ .-]?5(?:[ .-]?\d){8}|\+[1-9]\d{1,14})";
        public const string NationalNumber = "^[0-9]+$"; // General National Id
        public const string EgyptianNationalId = @"^(2|3)\d{13}$";
        public const string SaudiNationalId = @"^[12]\d{9}$";
        public const string IPCheck = @"^(?:(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4})$";
        public const string UrlCheck = @"^(https?:\/\/)([a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*|localhost|(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|\[[0-9a-fA-F:]+\])(?::(?:6553[0-5]|655[0-2][0-9]|65[0-4][0-9]{2}|6[0-4][0-9]{3}|[1-5][0-9]{4}|[1-9][0-9]{0,3}))?(\/[-a-zA-Z0-9()@:%_+.~#?&\/=]*)?$";
        public const string Sentence = @"(^[a-z])|\.\s+(.)";
        public const string HtmlTagSanitizerRegex = @"<(?!/?(b|i|span)\b)[^>]*>";
        public const string MediaTagsPattern = @"<(audio|video|img|object|iframe)\b[^>]*?((src|data)\s*=\s*(['""]).*?\4)?[^>]*?>";
        public const string BlockTagsPattern = @"<(br|p|div|li|tr|h[1-6])\b[^>]*>";
        public const string MediaWithContentPattern = @"<(audio|video|iframe|object)\b[^>]*>.*?</\1>";
        public const string AllTagsPattern = @"<.*?>";
        public const string NonUnicodeDigit = @"(?<!\\u[0-9a-fA-F]{0,3})[0-9]";
        public const string NonUrlNonUnicodeDigit = @"(?<!\\u[0-9a-fA-F]{0,3})(?<!http\S*)(?<!www\S*)[0-9]";
        public const string IsUrlOrEmail = @"(https?:\/\/|www\.|[\w._%+-]+@[\w.-]+\.[a-zA-Z]{2,})";
        public const string JsonStringValue = @"""([^""]*)""";
        public const string JsonStartPattern = @"^(\s*\{\s*|\s*\[\s*\{\s*)";
        public const string ArabicCharactersExpression = @"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]";
        public const string TempImageUrlRegex = @"(https?://[^""'\s]+)?/api/(?:temp-images/|AIAsset/GetAIAsset\?assetId=)([a-fA-F0-9\-]{36})";
        public const string LatexPlaceholderPattern = @"\[Latex\]\[(?<latex>.+?)\]";
        public const string TempLatexObjectPattern = @"<object[^>]*\sdata\s*=\s*""[^""]*[?&]assetId=(?<guid>[0-9a-fA-F\-]{36})[^""]*""[^>]*\sdata-latex\s*=\s*""(?<latexattr>[^""]*)""[^>]*>\s*</object>";
        public const string SimpleMathSuperscriptPattern = @"(?<base>[A-Za-z0-9\)\]])\^(?:\((?<value>[^()]+)\)|\{(?<valueBraced>[^{}]+)\}|(?<valueSimple>[+-]|\d*[+-]|-?[A-Za-z0-9]+))";
        public const string SimpleMathSubscriptPattern = @"(?<base>[A-Za-z0-9\)\]])_(?:\((?<value>[^()]+)\)|\{(?<valueBraced>[^{}]+)\}|(?<valueSimple>[A-Za-z0-9]+))";
        public const string LatexCommandPattern = @"\\[A-Za-z]+";
        public const string LimMissingBracesPattern = @"\\lim(?!_\{)";
        public const string BareExponentInLatexPattern = @"(?<![\^_A-Za-z0-9])([A-Za-z])(\d)(?![A-Za-z0-9])";
        public const string ImageAssetIdPattern = @"<img\b[^>]*\bsrc\s*=\s*[""'][^""']*assetId=([a-fA-F0-9\-]{36})[^""']*[""']";
        public const string ImgTagPattern = @"<img\b[^>]*>";
        public const string DollarLatexFallbackPattern = @"\${1,2}(?<latex>[^$]+?)\${1,2}";
        public const string BackslashRunBeforeLetterPattern = @"\\+(?=[A-Za-z])";
        public const string ChemicalEquationHintPattern = @"(→|\(aq\)|\(s\)|\(l\)|\(g\))";
        public const string ChemicalElementDigitPattern = @"(?<elem>Na|Mg|Al|Si|Cl|Ca|Fe|Cu|Zn|Ag|Au|Hg|Pb|Sn|Ni|Co|Cr|Mn|Ba|Sr|Li|Br|Kr|Xe|Ne|Ar|He|Rn|H|O|C|N|S|P|K|I|F|B)(?<digit>\d+)(?!\s*[+\-])";
    }
}
