using OES.Helper.Dtos.Subject;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Subject
{
    public interface IBlazSubjectService
    {
        Task<List<SubjectDto>> GetAllSubjectsAsync();

        Task<ApiResponse> CreateSubject(SubjectDto subjectDto);

        Task<CustomTableData<SubjectDto>> GetAllSubjectPaginationAsync(PaginationSearchModel pagination);

        Task<ApiResponse> DeleteSubject(long id);

        Task<SubjectDto> GetSubjectById(long id);

        Task<ApiResponse> UpdateSubject(SubjectDto subjectDto);
    }
}
