using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IUserSyncService
    {
        Task<IApiResponse> SyncUsers();
    }
}
