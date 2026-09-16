using OES.Helper.Dtos.Subject;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface ISubjectService
    {
        Task<IApiResponse> GetSubjectListAsync();
        Task<IApiResponse> GetPaginatedSubjects(PaginationSearchModel pagination);
        Task<IApiResponse> CreateSubject(SubjectDto subjectDto);
        Task<IApiResponse> UpdateSubject(SubjectDto subjectDto);
        Task<IApiResponse> GetSubjectById(long id);
        Task<IApiResponse> SoftDeleteSubject(long id);
        //Task<IApiResponse> GetUserSubjectGroupsAsync();
        //Task<IApiResponse> GetSubjectGroupsAsync(long SubjectId);
    }
}
