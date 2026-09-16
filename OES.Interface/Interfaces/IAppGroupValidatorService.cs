using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IAppGroupValidatorService
    {
        Task<ApiResponse> ValidateForInsertionAsync(NewGroupDto newGroupDto);

        Task<ApiResponse> ValidateForUpdateAsync(GroupUpdateDto groupUpdateDto);
    }
}
