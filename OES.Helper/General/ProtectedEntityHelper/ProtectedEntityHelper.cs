namespace OES.Helper.General.ProtectedEntityHelper
{
    public static class ProtectedEntityHelper
    {
        public static bool IsProtected(string incomingUser)
        {
            return !string.IsNullOrWhiteSpace(incomingUser) && string.Equals(incomingUser, DefaultSystemUser.Name, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsProtected(string incomingUser, GlobalUserContext.GlobalUserContext context)
        {
            return IsProtected(incomingUser) /*&& !context.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin)*/;
        }
    }
}
