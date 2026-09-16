namespace OES.Helper.General.CheckDuplicationClass
{
    public sealed class DuplicateFieldSpecDto
    {
        public string ErrorMessage { get; }

        public HashSet<string> Seen { get; }

        public string[] FieldNames { get; }

        public DuplicateFieldSpecDto(
            string errorMessage,
            HashSet<string> seen,
            params string[] fieldNames
        )
        {
            ErrorMessage = errorMessage;
            Seen = seen;
            FieldNames = fieldNames;
        }
    }
}
