using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.QualityCheckCommittee
{
    public interface IBlazQualityCheckCommitteeService
    {
        Task<CustomTableData<QualityCheckCommitteeDto>> GetAllCommitteesPaginatedAsync(PaginationSearchModel paginationSearch);

        Task<ApiResponse> GetAllCommitteesAsync();

        Task<ApiResponse> GetCommitteeByIdAsync(long id);

        Task<ApiResponse> GetCommitteeMembersAsync(long committeeId);

        Task<ApiResponse> CreateCommitteeWithMembersAsync(CreateQualityCheckCommitteeRequestDto dto);

        Task<ApiResponse> EditCommitteeAsync(QualityCheckCommitteeDto dto);

        //Task<List<GetOESGroupDto>> GetUserQualityCheckCommitteeGroupsAsync();

        //Task<QualityCheckCommitteeGroupDto> GetQualityCheckCommitteeGroupsAsync(long qualityCheckCommitteeId);
    }
}
