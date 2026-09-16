using OES.Helper.Dtos.DeltaType;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IDeltaTypeService
    {
        Task<IApiResponse> GetAllDeltaTypes();
        Task<IApiResponse> GetAllDeltaType(PaginationSearchModel pagination);
        Task<IApiResponse> GetDeltaTypeById(long id);
        Task<IApiResponse> AddDeltaType(AddDeltaTypeDto addDeltaTypeDto);
        Task<IApiResponse> UpdateDeltaType(GetDeltaTypeDto updateDeltaTypeDto);
        Task<IApiResponse> SoftDeleteDeltaType(long id);
        //Task<ApiResponse> GetUserDeltaTypeGroupsAsync();
        //Task<ApiResponse> GetDeltaTypeGroupsAsync(long deltaTypeId);
    }
}
