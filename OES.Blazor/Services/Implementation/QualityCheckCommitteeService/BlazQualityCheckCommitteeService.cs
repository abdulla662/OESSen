using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.QualityCheckCommittee;
using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.QualityCheckCommitteeService
{
    public class BlazQualityCheckCommitteeService : IBlazQualityCheckCommitteeService
    {
        private readonly IHttpClientHelper _httpClient;
        private readonly IBlazGetCustomTableData<QualityCheckCommitteeDto> _blazGetCustomTableData;

        public BlazQualityCheckCommitteeService(IHttpClientHelper httpClient, IBlazGetCustomTableData<QualityCheckCommitteeDto> blazGetCustomTableData)
        {
            _httpClient = httpClient;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<CustomTableData<QualityCheckCommitteeDto>> GetAllCommitteesPaginatedAsync(PaginationSearchModel paginationSearch)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(paginationSearch, "api/QualityCheckCommittee/getAllCommitteesPaginated");

            return response;
        }

        public async Task<ApiResponse> GetAllCommitteesAsync()
        {
            return await _httpClient.GetAsync<List<QualityCheckCommitteeDto>>("api/QualityCheckCommittee/getAllCommittees");
        }

        public async Task<ApiResponse> GetCommitteeByIdAsync(long id)
        {
            return await _httpClient.GetAsync<QualityCheckCommitteeDto>($"api/QualityCheckCommittee/getCommitteeById?{nameof(id)}={id}");
        }

        public async Task<ApiResponse> GetCommitteeMembersAsync(long committeeId)
        {
            return await _httpClient.GetAsync<List<QualityCheckCommitteeMemberDto>>($"api/QualityCheckCommittee/getCommitteeMembers?{nameof(committeeId)}={committeeId}");
        }

        public async Task<ApiResponse> CreateCommitteeWithMembersAsync(CreateQualityCheckCommitteeRequestDto dto)
        {
            return await _httpClient.PostAsync(dto, "api/QualityCheckCommittee/createCommitteeWithMembers");
        }

        public async Task<ApiResponse> EditCommitteeAsync(QualityCheckCommitteeDto dto)
        {
            return await _httpClient.PostAsync(dto, "api/QualityCheckCommittee/editCommitteeAsync");
        }

        //public async Task<List<GetOESGroupDto>> GetUserQualityCheckCommitteeGroupsAsync()
        //{
        //    var response = await _httpClient.GetAsync<List<GetOESGroupDto>>("api/QualityCheckCommittee/GetUserQualityCheckCommitteeGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<QualityCheckCommitteeGroupDto> GetQualityCheckCommitteeGroupsAsync(long qualityCheckCommitteeId)
        //{
        //    var response = await _httpClient.GetAsync<QualityCheckCommitteeGroupDto>($"api/QualityCheckCommittee/GetQualityCheckCommitteeGroupsAsync?qualityCheckCommitteeId={qualityCheckCommitteeId}");
        //    return response.Data as QualityCheckCommitteeGroupDto ?? new QualityCheckCommitteeGroupDto();
        //}
    }
}
