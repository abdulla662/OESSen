namespace OES.Helper.Static
{
    public static class TrueFalseValidationConstants
    {
        public static IReadOnlySet<string> ValidTrueKeywords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "true",
            "t",
            "yes",
            "y",
            "صح",
            "نعم",
            "صواب"
        };

        public static IReadOnlySet<string> ValidFalseKeywords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "false",
            "f",
            "no",
            "n",
            "خطأ",
            "لا",
            "خطا"
        };

        public static IReadOnlySet<string> AllValidKeywords { get; }

        static TrueFalseValidationConstants()
        {
            var allKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            allKeywords.UnionWith(ValidTrueKeywords);
            allKeywords.UnionWith(ValidFalseKeywords);
            AllValidKeywords = allKeywords;
        }
    }
}
