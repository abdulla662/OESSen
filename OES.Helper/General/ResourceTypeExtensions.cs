using OES.Helper.Enums;

namespace OES.Helper.General
{
    public static class ResourceTypeExtensions
    {
        public static string GetRolePrefix(this ResourceType type)
        {
            var field = type.GetType().GetField(type.ToString());

            var attribute = field?.GetCustomAttributes(typeof(RolePrefixAttribute), false).FirstOrDefault() as RolePrefixAttribute;

            var prefix = attribute?.Prefix ?? type.ToString();

            if (prefix.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                prefix = prefix[..^1];

            return prefix;
        }
    }
}
