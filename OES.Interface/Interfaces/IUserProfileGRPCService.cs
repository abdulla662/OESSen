using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IUserProfileGRPCService
    {
        Task<ApiResponse> GetAllOrganizationUserProfile(UserParamsForSync userParamsForSync);
    }
}
