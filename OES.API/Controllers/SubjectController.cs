using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Subject;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{

    public class SubjectController : OESBaseController
    {
        private readonly ISubjectService _subjectService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubjectController"/> class.
        /// </summary>
        /// <param name="subjectService">The service for handling question-related operations.</param>
        public SubjectController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        /// <summary>
        /// Retrieve all subjects as a list
        /// </summary>
        /// <returns>Returns a list of all roles.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetAllSubjectList")]
        public async Task<IApiResponse> GetAllSubjectList()
        {
            return await _subjectService.GetSubjectListAsync();
        }

        [OESFilter(Authorize = true)]
        [HttpPost("PaginatedSubjects")]
        public async Task<IApiResponse> GetPaginatedSubjects([FromBody] PaginationSearchModel pagination)
        {
            return await _subjectService.GetPaginatedSubjects(pagination);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("CreateSubject")]
        public async Task<IApiResponse> CreateSubject(SubjectDto subjectDto)
        {
            return await _subjectService.CreateSubject(subjectDto);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("UpdateSubject")]
        public async Task<IApiResponse> UpdateSubject(SubjectDto subjectDto)
        {
            return await _subjectService.UpdateSubject(subjectDto);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetSubjectById")]
        public async Task<IApiResponse> GetSubjectById(long id)
        {
            return await _subjectService.GetSubjectById(id);
        }

        [OESFilter(Authorize = true)]
        [HttpDelete("SoftDeleteSubject")]
        public async Task<IApiResponse> SoftDeleteSubject(long id)
        {
            return await _subjectService.SoftDeleteSubject(id);
        }

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetUserSubjectGroupsAsync")]
        //public async Task<IApiResponse> GetUserSubjectGroupsAsync()
        //{
        //    return await _subjectService.GetUserSubjectGroupsAsync();
        //}

        //[OESFilter(Authorize = true)]
        //[HttpGet("GetSubjectGroupsAsync")]
        //public async Task<IApiResponse> GetSubjectGroupsAsync(long subjectId)
        //{ 
        //    return await _subjectService.GetSubjectGroupsAsync(subjectId);
        //}
    }
}
