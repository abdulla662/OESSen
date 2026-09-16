namespace OES.Blazor.Services.Interfaces.ISessionStorageService
{
    public interface IBlazSessionStorageService
    {
        Task<bool> SetValue(string key, object value, bool WithEncrypt = true);

        Task<T> GetValue<T>(string key, bool WithEncrypt = true);

        Task<bool> RemoveValue(string key);
    }
}
