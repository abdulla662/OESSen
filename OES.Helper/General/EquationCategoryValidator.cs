using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.ResourceFiles;
using System.Text.RegularExpressions;

namespace OES.Helper.General
{
    public static class EquationCategoryValidator
    {
        private static readonly Regex _functionPattern = new(@"(NC|ND|NT)\($", RegexOptions.IgnoreCase);
        private static readonly Regex _categoryPattern = new(@"\[([^\]]+)\]");

        public static (bool isValid, string errorMessage) ValidateEquationCategoryReferences(List<EquationDto> equations)
        {
            foreach (var equation in equations)
            {
                var matches = _categoryPattern.Matches(equation.Equation);

                foreach (Match match in matches)
                {
                    var referencedName = match.Groups[1].Value;

                    bool isInsideFunction = IsMatchInsideFunction(equation.Equation, match.Index);

                    if (!isInsideFunction)
                    {
                        bool categoryExists = equations.Any(e =>
                            e.Id != equation.Id &&
                            e.Name.Equals(referencedName, StringComparison.OrdinalIgnoreCase)
                        );

                        if (!categoryExists)
                        {
                            return (false, string.Format(
                                Resource.CategoryReferencesNonExistentCategory,
                                equation.Name,
                                referencedName
                            ));
                        }
                    }
                }
            }

            return (true, string.Empty);
        }

        private static bool IsMatchInsideFunction(string equation, int matchIndex)
        {
            if (matchIndex < 3) return false;

            var beforeMatch = equation.Substring(0, matchIndex);

            return _functionPattern.IsMatch(beforeMatch);
        }

        public static string BuildDisplayEquation(string equation, Func<long, string> idToNameResolver)
        {
            if (string.IsNullOrWhiteSpace(equation)) return string.Empty;

            return Regex.Replace(equation, @"\[(\d+)\]", match =>
            {
                if (!long.TryParse(match.Groups[1].Value, out long id)) return match.Value;
                var name = idToNameResolver(id);
                return name != null ? $"[{name}]" : match.Value;
            });
        }
    }
}