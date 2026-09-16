using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IQualityCheckCommitteeService
    {
        Task<ApiResponse> GetAllQualityCheckCommitteePaginatedListAsync(PaginationSearchModel pagination);

        Task<ApiResponse> GetAllQualityCheckCommitteeAsync();

        Task<ApiResponse> GetQualityCheckCommitteeByIdAsync(long id);

        Task<ApiResponse> GetQualityCheckCommitteeMembersAsync(long committeeId);

        Task<ApiResponse> CreateCommitteeWithMembersAsync(CreateQualityCheckCommitteeRequestDto dto);

        Task<ApiResponse> EditQualityCheckCommitteeByIdAsync(QualityCheckCommitteeDto dto);

        //Task<ApiResponse> GetUserQualityCheckCommitteeGroupsAsync();

        //Task<ApiResponse> GetQualityCheckCommitteeGroupsAsync(long qualityCheckCommitteeId);
    }
}
