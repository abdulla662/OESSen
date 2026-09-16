using Blazored.SessionStorage;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.SessionStorageService
{
    public class SessionStorageService : IBlazSessionStorageService
    {
        private readonly ISessionStorageService _sessionStorageService;

        public SessionStorageService(ISessionStorageService sessionStorageService)
        {
            _sessionStorageService = sessionStorageService;
        }

        public async Task<T> GetValue<T>(string key, bool WithEncrypt = true)
        {
            string value = null;

            try
            {
                value = await _sessionStorageService.GetItemAsStringAsync(key);

                if (string.IsNullOrEmpty(value))
                {
                    return (T)Convert.ChangeType("0", typeof(T));
                }

                value = value.Trim('"');

                if (Guid.TryParse(value, out Guid result))
                {
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)value;
                    }

                    if (typeof(T) == typeof(Guid))
                    {
                        return (T)(object)result;
                    }

                    return (T)Convert.ChangeType(result, typeof(T));
                }
                else
                {
                    if (WithEncrypt)
                    {
                        var DecriptedValue = AesCipher.Decrypt(value);
                        DecriptedValue = DecriptedValue.Replace('\'', ' ');
                        return (T)Convert.ChangeType(DecriptedValue, typeof(T));
                    }

                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[SessionStorageService.GetValue:{key}] raw='{value}' len={value?.Length} err={ex.GetType().Name}: {ex.Message}");
                return (T)Convert.ChangeType("0", typeof(T));
            }


            // WARNING: This commented code has a missed case, DON'T USE IT.

            //var encrypted = await _sessionStorageService.GetItemAsStringAsync(key);

            //if (encrypted != null)
            //{
            //    encrypted = encrypted.Trim('"');
            //
            //    if (WithEncrypt)
            //    {                   
            //        var DecriptedValue = CryptoHelper.Decrypt(encrypted);
            //        DecriptedValue = DecriptedValue.Replace('\'' , ' ');
            //        return (T)Convert.ChangeType(DecriptedValue, typeof(T));
            //    }
            //    else
            //    {
            //        return (T)Convert.ChangeType(encrypted, typeof(T));
            //    }
            //}
            //else
            //{
            //    return (T)Convert.ChangeType("0", typeof(T));
            //}
        }

        public async Task<bool> RemoveValue(string key)
        {
            try
            {
                await _sessionStorageService.RemoveItemAsync(key);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetValue(string key, object value, bool WithEncrypt = true)
        {
            try
            {
                if (Guid.TryParse(value.ToString(), out Guid result))
                {
                    await _sessionStorageService.SetItemAsStringAsync(key, value.ToString());
                }
                else if (WithEncrypt)
                {
                    await _sessionStorageService.SetItemAsStringAsync(key, AesCipher.Encrypt(value.ToString()));
                }
                else
                {
                    await _sessionStorageService.SetItemAsStringAsync(key, value.ToString());
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}