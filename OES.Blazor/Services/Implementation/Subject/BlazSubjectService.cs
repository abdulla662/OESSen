using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Subject;
using OES.Helper.Dtos.Subject;
using OES.Helper.General;

namespace OES.Blazor.Services.Implementation.Subject
{
    public class BlazSubjectService : IBlazSubjectService
    {
        private readonly IHttpClientHelper _httpClient;

        private readonly IBlazGetCustomTableData<SubjectDto> _blazGetCustomTableData;

        public BlazSubjectService(IHttpClientHelper httpClient, IBlazGetCustomTableData<SubjectDto> blazGetCustomTableData)
        {
            _httpClient = httpClient;
            _blazGetCustomTableData = blazGetCustomTableData;
        }

        public async Task<List<SubjectDto>> GetAllSubjectsAsync()
        {
            var response = await _httpClient.GetAsync<List<SubjectDto>>("api/Subject/GetAllSubjectList");

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.Data as List<SubjectDto>;
            }

            return [];
        }

        public async Task<ApiResponse> CreateSubject(SubjectDto subjectDto)
        {
            var response = await _httpClient.PostAsync(subjectDto, "api/Subject/CreateSubject");

            return response;
        }

        public async Task<CustomTableData<SubjectDto>> GetAllSubjectPaginationAsync(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableData.GetCustomTableData(pagination, "api/Subject/PaginatedSubjects");
        }

        public async Task<ApiResponse> DeleteSubject(long id)
        {
            return await _httpClient.DeleteAsync($"api/Subject/SoftDeleteSubject?id={id}");
        }

        public async Task<ApiResponse> UpdateSubject(SubjectDto subjectDto)
        {
            return await _httpClient.PostAsync(subjectDto, $"api/Subject/UpdateSubject");
        }

        public async Task<SubjectDto> GetSubjectById(long id)
        {
            var result = await _httpClient.GetAsync<SubjectDto>($"api/Subject/GetSubjectById?id={id}");

            return (SubjectDto)result.Data;
        }

        //public async Task<List<GetOESGroupDto>> GetUserSubjectGroupsAsync()
        //{
        //    var response = await _httpClient.GetAsync<List<GetOESGroupDto>>("api/Subject/GetUserSubjectGroupsAsync");
        //    return (List<GetOESGroupDto>)response.Data ?? [];
        //}

        //public async Task<SubjectGroupDto> GetSubjectGroupsAsync(long subjectId)
        //{
        //    var response = await _httpClient.GetAsync<SubjectGroupDto>($"api/Subject/GetSubjectGroupsAsync?subjectId={subjectId}");
        //    return response.Data as SubjectGroupDto ?? new SubjectGroupDto();
        //}
    }
}
