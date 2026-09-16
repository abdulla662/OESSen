namespace OES.Helper.General
{
    [AttributeUsage(AttributeTargets.Field)]
    public class RolePrefixAttribute : Attribute
    {
        public string Prefix { get; }

        public RolePrefixAttribute(string prefix)
        {
            Prefix = prefix;
        }
    }
}
