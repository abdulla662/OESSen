using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.General;

namespace OES.Blazor.Extensions
{
    public static class CrudSessionExtensions
    {
        public static async Task SetCrudSessionAsync(
            this IBlazSessionStorageService sessionStorage,
            long id,
            string actionKey,
            bool isCreatePage = false
        )
        {
            await sessionStorage.SetValue(MiscConstants.IsCreatePage, isCreatePage);
            await sessionStorage.SetValue(actionKey, id.ToString() ?? string.Empty);
        }
    }
}
