using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OES.Helper.Enums.EnumExtensions
{
    public static class EnumExtensions
    {
        public static string GetStringEnumValueAsDisplayName<TEnum>(this string enumValueAsString) where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(enumValueAsString, out var enumValue))
                return enumValue.GetEnumValueAsDisplayName();

            return enumValueAsString;
        }

        public static string GetEnumValueAsDisplayName(this Enum enumValue)
        {
            var enumMember = enumValue
                .GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault();

            if (enumMember != null)
            {
                var displayAttr = enumMember.GetCustomAttribute<DisplayAttribute>();

                if (displayAttr != null)
                    return displayAttr.GetName();
            }

            return Regex.Replace(enumValue.ToString(), "(\\B[A-Z])", " $1");
        }
    }
}
