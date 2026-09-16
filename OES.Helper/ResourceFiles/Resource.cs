using System.Resources;

namespace OES.Helper.ResourceFiles
{
    public partial class Resource
    {
        public static string PropertyLocalization(string propertyName)
        {
            propertyName ??= string.Empty;

            var localizedName = ResourceManager.GetString(propertyName, resourceCulture);

            return localizedName ?? propertyName;
        }
    }
}
