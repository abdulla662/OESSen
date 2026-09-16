using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class DisabilityService : IDisabilityService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;

        public DisabilityService(ICommonService commonService, IMapper mapper)
        {
            _commonService = commonService;
            _mapper = mapper;
        }

        public async Task<ApiResponse> AddCandidateExtraTimeAsync(AddCandidateExtraTimeDto addCandidateExtraTimeDto)
        {
            if (addCandidateExtraTimeDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NotFound
                );
            }

            var candidateRepo = _commonService._unitOfWork.Repository<Candidate, long>();

            var candidate = await candidateRepo.GetByIdAsync(addCandidateExtraTimeDto.CandidateId);

            if (candidate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NotFound
                );
            }

            if (addCandidateExtraTimeDto.DisabilityId == 0)
            {
                candidate.HasDisability = false;
                candidate.DisabilityId = null;
            }
            else
            {
                candidate.HasDisability = true;
                candidate.DisabilityId = addCandidateExtraTimeDto.DisabilityId;
                candidate.IsSynced = false;
            }

            candidateRepo.Update(candidate);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.Successfully);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                              HttpStatusCode.InternalServerError,
                                                              Resource.Failed);
        }

        public async Task<ApiResponse> AddDisabilityAsync(AddOrUpdateDisabilityDto addOrUpdateDisabilityDto)
        {
            if (addOrUpdateDisabilityDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NotFound
                );
            }

            var disabilityRepo = _commonService._unitOfWork.Repository<Disability, long>();

            var normalizedName = addOrUpdateDisabilityDto.Name.Trim().ToLower();

            if (await disabilityRepo.IsExistAsync(x => x.Name.Trim().ToLower() == normalizedName))
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.AlreadyExist,
                                    HttpStatusCode.Conflict,
                                    Resource.DisabilityNameAlreadyExists);
            }

            var mapData = _mapper.Map<Disability>(addOrUpdateDisabilityDto);

            await disabilityRepo.AddAsync(mapData);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.Successfully);
            }
            else
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.Failed);
            }
        }

        public async Task<ApiResponse> DeleteDisabilityAsync(long id)
        {
            var disability = await _commonService._unitOfWork.Repository<Disability, long>().GetByIdAsync(id);

            if (disability == null)
            {
                return _commonService
                      ._apiResponse
                      .GetApiResponse(CustomCodeStatus.NotFound,
                                      HttpStatusCode.NotFound,
                                      Resource.Failed);
            }

            var assignedToCandidate = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .IsExistAsync(candidate => candidate.DisabilityId == id);

            if (assignedToCandidate)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Conflict,
                                    HttpStatusCode.Conflict,
                                    Resource.DisabilityAssignedToCandidateCannotBeDeleted);
            }

            _commonService._unitOfWork.Repository<Disability, long>().Delete(disability);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.Successfully);
            }
            else
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.Failed);
            }
        }

        public async Task<ApiResponse> GetAllDisabilitiesAsync()
        {
            var disabilities = await _commonService._unitOfWork.Repository<Disability, long>().GetAllAsync();

            if (!disabilities.Any())
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NotFound);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.Success, disabilities);
        }

        public async Task<ApiResponse> GetDisabilityByIdAsync(long id)
        {
            var disability = await _commonService._unitOfWork.Repository<Disability, long>().GetByIdAsync(id);

            if (disability == null)
            {
                return _commonService
                       ._apiResponse
                       .GetApiResponse(CustomCodeStatus.NotFound,
                                       HttpStatusCode.NotFound,
                                       Resource.NotFound);
            }

            var mapData = _mapper.Map<AddOrUpdateDisabilityDto>(disability);

            if (mapData != null)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.Successfully,
                                        mapData);
            }
            else
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.Failed);
            }
        }

        public async Task<ApiResponse> GetAllDisabilitiesForPaginationAsync(PaginationSearchModel searchModel)
        {
            var query = _commonService._unitOfWork.Repository<Disability, long>().GetAll();

            if (!string.IsNullOrEmpty(searchModel.SearchKey))
            {
                if (searchModel.SearchInName && searchModel.SearchInDescription)
                {
                    query = query.Where(x => x.Name.Contains(searchModel.SearchKey) || x.Description.Contains(searchModel.SearchKey));
                }
                else if (searchModel.SearchInName)
                {
                    query = query.Where(x => x.Name.Contains(searchModel.SearchKey));
                }
                else if (searchModel.SearchInDescription)
                {
                    query = query.Where(x => x.Description.Contains(searchModel.SearchKey));
                }
            }

            if (searchModel.FromDate is not null)
            {
                query = query.Where(o => o.CreationDate >= searchModel.FromDate &&
                                    o.CreationDate <= (searchModel.ToDate ?? DateTime.Now.Date));
            }

            query = searchModel.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var countItems = await query.CountAsync();

            var data = await query
                .Skip(searchModel.PageIndex * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .ToListAsync();

            var mapData = _mapper.Map<List<AddOrUpdateDisabilityDto>>(data);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              string.Empty,
                                                              new CustomTableData<AddOrUpdateDisabilityDto>(mapData, countItems));
        }

        public async Task<ApiResponse> UpdateDisabilityAsync(AddOrUpdateDisabilityDto addOrUpdateDisability)
        {
            var disabilityRepo = _commonService._unitOfWork.Repository<Disability, long>();
            var isExisting = await disabilityRepo.GetByIdAsync((long)addOrUpdateDisability.Id);

            if (isExisting == null)
            {
                return _commonService
                       ._apiResponse
                       .GetApiResponse(CustomCodeStatus.NotFound,
                                       HttpStatusCode.NotFound,
                                       Resource.Failed);
            }

            var normalizedName = addOrUpdateDisability.Name.Trim().ToLower();

            if (await disabilityRepo.IsExistAsync(x => x.Id != isExisting.Id && x.Name.Trim().ToLower() == normalizedName))
            {
                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.AlreadyExist,
                                   HttpStatusCode.Conflict,
                                   Resource.DisabilityNameAlreadyExists);
            }

            isExisting.Name = addOrUpdateDisability.Name;
            isExisting.Description = addOrUpdateDisability.Description;
            isExisting.ExtraTimePercentage = addOrUpdateDisability.ExtraTimePercentage;

            disabilityRepo.Update(isExisting);

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.Successfully);
            }
            else
            {
                return _commonService
                       ._apiResponse
                       .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                       HttpStatusCode.InternalServerError,
                                       Resource.Failed);
            }
        }
    }
}
