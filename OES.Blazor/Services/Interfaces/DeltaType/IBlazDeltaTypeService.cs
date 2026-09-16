using OES.Helper.Dtos.DeltaType;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.DeltaType
{
    public interface IBlazDeltaTypeService
    {
        Task<List<GetDeltaTypeDto>> GetDeltaTypes();
        Task<CustomTableData<GetDeltaTypeDto>> GetAllDeltaTypesPaginated(PaginationSearchModel pagination);
        Task<GetDeltaTypeDto> GetDeltaTypeById(long id);
        Task<ApiResponse> AddDeltaType(AddDeltaTypeDto addDeltaType);
        Task<ApiResponse> UpdateDeltaType(GetDeltaTypeDto updateDeltaType);
        Task<ApiResponse> SoftDeleteDeltaType(long id);
        //Task<List<GetOESGroupDto>> GetUserDeltaTypeGroupsAsync();
        //Task<DeltaTypeGroupDto> GetDeltaTypeGroupsAsync(long deltaTypeId);
    }
}
