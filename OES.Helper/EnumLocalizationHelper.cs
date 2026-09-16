using OES.Helper.ResourceFiles;

namespace OES.Helper
{
    public static class EnumLocalizationHelper
    {
        public static string ToLocalizedString(this Enum enumValue)
        {
            string resourceKey = $"{enumValue.GetType().Name}{enumValue}";

            string localizedValue = Resource.ResourceManager.GetString(resourceKey);

            return string.IsNullOrWhiteSpace(localizedValue) ? enumValue.ToString() : localizedValue;
        }

        public static string ToLocalizedString<TEnum>(this string enumName) where TEnum : Enum
        {
            if (Enum.TryParse(typeof(TEnum), enumName, out var enumValue))
            {
                return ((TEnum)enumValue).ToLocalizedString();
            }

            return enumName;
        }
    }
}
