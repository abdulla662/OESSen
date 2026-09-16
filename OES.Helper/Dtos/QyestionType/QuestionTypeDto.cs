using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace OES.Helper.Dtos.QyestionType
{
    public class QuestionTypeDto
    {
        public long Id { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.NameIsRequired))]
        public string Name { get; set; } = string.Empty;

        public string DisplayName => SplitPascalCase(Name); // Note: This method is referenced using reflection, don't delete it, as it has 0 references.

        private static string SplitPascalCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            return Regex.Replace(input, "(\\B[A-Z])", " $1");
        }
    }
}
