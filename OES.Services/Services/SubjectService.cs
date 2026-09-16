using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.Subject;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class SubjectService(ICommonService _commonService, IMapper _mapper, FilterParamsValues _filterParamsValues) : ISubjectService
    {
        public async Task<IApiResponse> GetPaginatedSubjects(PaginationSearchModel pagination)
        {
            var query = _commonService._unitOfWork.Repository<Subject, long>().GetAll().AsNoTracking();

            if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInName)
            {
                query = query.Where(x => x.Name.Contains(pagination.SearchKey));
            }

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                         o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalItems = await query.CountAsync();

            var paginatedData = await query.Skip(pagination.PageIndex * pagination.PageSize)
                                           .Take(pagination.PageSize)
                                           .ToListAsync();

            var subjectDtos = _mapper.Map<List<SubjectDto>>(paginatedData);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Subjects fetched successfully",
                new CustomTableData<SubjectDto>(subjectDtos, totalItems)
            );
        }

        public async Task<IApiResponse> GetSubjectListAsync()
        {
            var query = _commonService._unitOfWork.Repository<Subject, long>().GetAllAsync();

            var data = await query;

            var mappedData = _commonService._mapper.Map<List<SubjectDto>>(data);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                mappedData
            );
        }

        public async Task<IApiResponse> CreateSubject(SubjectDto subjectDto)
        {
            var existingSubject = await _commonService._unitOfWork
                .Repository<Subject, long>()
                .GetObjAsync(s => s.Name.Trim().ToLower() == subjectDto.Name.Trim().ToLower());

            if (existingSubject != null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    Resource.SubjectNameAlreadyExists
                );
            }

            var subjectEntity = _mapper.Map<Subject>(subjectDto);

            await _commonService._unitOfWork.Repository<Subject, long>().AddAsync(subjectEntity);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SubjectCreatedSuccessfully);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.FailtocreateSubject
                );
            }
        }

        public async Task<IApiResponse> UpdateSubject(SubjectDto subjectDto)
        {
            var existingSubject = await _commonService._unitOfWork
                .Repository<Subject, long>()
                .GetObjAsync(s => s.Name.Trim().ToLower() == subjectDto.Name.Trim().ToLower() && s.Id != subjectDto.Id);

            if (existingSubject != null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    Resource.SubjectNameAlreadyExists
                );
            }

            var subjectEntity = await _commonService._unitOfWork.Repository<Subject, long>().GetObjAsync(x => x.Id == subjectDto.Id);

            if (subjectEntity == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "Subject not found");
            }

            subjectEntity.Name = subjectDto.Name;
            subjectEntity.ModeficationDate = DateTimeHelper.Now;

            _commonService._unitOfWork.Repository<Subject, long>().Update(subjectEntity);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.subjectupdatedsuccessfully);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    "Unable to update the subject. Please try again later.");
            }
        }

        public async Task<IApiResponse> GetSubjectById(long id)
        {
            var subjectEntity = await _commonService._unitOfWork.Repository<Subject, long>().GetObjAsync(x => x.Id == id);

            if (subjectEntity == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "Subject not found"
                );
            }

            var subjectDto = _mapper.Map<SubjectDto>(subjectEntity);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Subject fetched successfully",
                subjectDto
            );
        }

        public async Task<IApiResponse> SoftDeleteSubject(long id)
        {
            var subject = _commonService._unitOfWork.Repository<Subject, long>();

            var subjectData = await subject.GetObjAsync(x => x.Id == id);

            if (subjectData != null)
            {
                string checkingSubjectDependenciesResult = await CheckingSubjectDependenciesAsync(subjectData.Id);

                if (!string.IsNullOrWhiteSpace(checkingSubjectDependenciesResult))
                {
                    return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Conflict,
                                            HttpStatusCode.Conflict,
                                            checkingSubjectDependenciesResult);
                }

                subject.SoftDelete(subjectData);

                await _commonService._unitOfWork.Complete();

                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        "Subject has been deleted successfully",
                                        subjectData);
            }
            else
            {
                return _commonService
                                    ._apiResponse
                                    .GetApiResponse(CustomCodeStatus.NotFound,
                                                    HttpStatusCode.NotFound,
                                                    "Subject not found");
            }
        }

        //public async Task<IApiResponse> GetUserSubjectGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Material) &&
        //        (
        //            g.CreationUser.ToLower() == currentUser ||
        //            g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
        //        ),
        //        Including: nameof(OESGroup.GroupResources)
        //    );

        //    var groupDtos = groups
        //        .Select(g => new GetOESGroupDto { Id = g.Id, Name = g.Name })
        //        .DistinctBy(x => x.Id)
        //        .OrderBy(x => x.Name)
        //        .ToList();

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, groupDtos);
        //}

        //public async Task<IApiResponse> GetSubjectGroupsAsync(long SubjectId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<SubjectGroups, long>()
        //        .GetAllAsync(
        //            x => x.SubjectId == SubjectId && !x.OESGroup.IsTemplate,
        //            Including: nameof(SubjectGroups.OESGroup)
        //        );

        //    var dto = new SubjectGroupDto();
        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));
        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}


        #region Helper Methods
        private async Task<string> CheckingSubjectDependenciesAsync(long subjectId)
        {
            var hasAssociatedQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AnyAsync(q => q.SubjectId == subjectId);

            if (hasAssociatedQuestions)
            {
                return Resource.SubjectCannotBeDeletedDueToQuestions;
            }

            var hasAssociatedPapers = await _commonService
                ._unitOfWork
                .Repository<PaperSubject, long>()
                .Query()
                .AnyAsync(q => q.SubjectId == subjectId);

            if (hasAssociatedPapers)
            {
                return Resource.SubjectCannotBeDeletedDueToPapers;
            }

            var hasAssociatedUsers = await _commonService
                ._unitOfWork
                .Repository<AppUserProfileSubject, long>()
                .Query()
                .AnyAsync(q => q.SubjectId == subjectId);

            if (hasAssociatedUsers)
            {
                return Resource.SubjectCannotBeDeletedDueToUsers;
            }

            return null;
        }
        #endregion
    }
}
