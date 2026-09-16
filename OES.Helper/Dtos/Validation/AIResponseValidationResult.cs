namespace OES.Helper.Dtos.Validation
{
    public sealed class AIResponseValidationResult
    {
        public bool IsValid { get; init; }

        public IReadOnlyList<string> Errors { get; init; } = [];

        public static AIResponseValidationResult Success() => new() { IsValid = true };

        public static AIResponseValidationResult Fail(params string[] errors) => new() { IsValid = false, Errors = errors };

        public static AIResponseValidationResult Fail(IEnumerable<string> errors) => new() { IsValid = false, Errors = [.. errors] };
    }
}
