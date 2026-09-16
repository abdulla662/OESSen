using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using OES.Helper.Dtos.Paper.TransitionDtos.Response;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class TransitionLevelService(ICommonService _commonService, IMapper _mapper) : ITransitionLevelService
    {
        public async Task<IApiResponse> GetAllTransitionLevel(PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<TransitionLevel, long>()
                .GetAll()
                .Include(x => x.TransitionProfile)
                .Include(x => x.QuestionCategory)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInName)
                query = query.Where(x => x.Name.Contains(pagination.SearchKey));

            if (pagination.FromDate is not null)
                query = query.Where(o => o.CreationDate >= pagination.FromDate && o.CreationDate < ((pagination.ToDate ?? DateTime.Today).AddDays(1)));

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var countItems = await query.CountAsync();

            var data = await query
                .Skip(pagination.PageIndex * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            var mapData = _mapper.Map<List<TransitionLevelDto>>(data);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              new CustomTableData<TransitionLevelDto>(mapData, countItems));
        }

        public async Task<IApiResponse> GetTransitionLevelById(long id)
        {
            if (await _commonService._unitOfWork.Repository<TransitionLevel, long>().IsExistAsync(e => e.Id == id))
            {
                var transitionLevel = await _commonService
                ._unitOfWork
                .Repository<TransitionLevel, long>()
                .GetObjAsync(
                e => e.Id == id,
                Including:
                    $"{nameof(TransitionLevel.TransitionProfile)}," +
                    $"{nameof(TransitionLevel.TransitionProfile)}.{nameof(TransitionProfile.DifficultyProfile)}," +
                    $"{nameof(TransitionLevel.DifficultyLevel)}," +
                    $"{nameof(TransitionLevel.QuestionCategory)}"
                );

                if (transitionLevel is null)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.TransitionLevelNotFound);
                }

                var dto = _mapper.Map<GetTransitionLevelDto>(transitionLevel);

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null,
                                    dto);
            }
            else
            {
                return _commonService
                                ._apiResponse
                                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                HttpStatusCode.InternalServerError,
                                                "An error occurred while fetching the transition");
            }
        }

        public async Task<IApiResponse> GetTransitionLevelsByProfileId(long profileId)
        {
            var query = await _commonService
                ._unitOfWork
                .Repository<TransitionLevel, long>()
                .Query()
                .AsNoTracking()
                .Include(x => x.QuestionCategory)
                .Where(x => x.TransitionProfileId == profileId)
                .ToListAsync();

            var mapData = _mapper.Map<List<GetTransitionLevelsDto>>(query);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              mapData);
        }

        public async Task<IApiResponse> AddTransitionLevel(AddTransitionLevelRequestDto _dto)
        {
            var (isDuplicate, duplicateMessage) = await CheckDuplicateDifficultyLevelAsync(
               _dto.TransitionProfileId,
               _dto.QuestionCategoryId,
               _dto.DifficultyLevelId
            );

            if (isDuplicate)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.BadRequest,
                                        duplicateMessage);
            }

            var (hasOverlap, overlapMessage) = await CheckOverlapWithinCategoryAsync(
                _dto.TransitionProfileId,
                _dto.QuestionCategoryId,
                _dto.LowerDScore,
                _dto.UpperDScore,
                _dto.DifficultyLevelId
            );

            if (hasOverlap)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.BadRequest,
                                        overlapMessage);
            }

            var mappedDelta = _mapper.Map<TransitionLevel>(_dto);

            await _commonService._unitOfWork.Repository<TransitionLevel, long>().AddAsync(mappedDelta);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Success,
                                            HttpStatusCode.OK,
                                            Resource.TransitionLevelAddedSuccessfully);
            }
            else
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.InternalServerError,
                                            Resource.Failedtoaddtransitionlevel);
            }
        }

        public async Task<IApiResponse> UpdateTransitionLevel(GetTransitionLevelDto _updateDelta)
        {
            var transictionLevel = await _commonService
                ._unitOfWork
                .Repository<TransitionLevel, long>()
                .GetObjAsync(e => e.Id == _updateDelta.Id);

            if (transictionLevel == null)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.TransitionLevelNotFound);
            }

            var (isDuplicate, duplicateMessage) = await CheckDuplicateDifficultyLevelAsync(
                _updateDelta.TransitionProfileId,
                _updateDelta.QuestionCategoryId,
                _updateDelta.DifficultyLevelId,
                _updateDelta.Id
            );

            if (isDuplicate)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.BadRequest,
                                        duplicateMessage);
            }

            var (hasOverlap, overlapMessage) = await CheckOverlapWithinCategoryAsync(
                _updateDelta.TransitionProfileId,
                _updateDelta.QuestionCategoryId,
                _updateDelta.LowerDScore,
                _updateDelta.UpperDScore,
                _updateDelta.DifficultyLevelId,
                _updateDelta.Id
            );

            if (hasOverlap)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.BadRequest,
                                            overlapMessage);
            }

            _mapper.Map(_updateDelta, transictionLevel);

            _commonService._unitOfWork.Repository<TransitionLevel, long>().Update(transictionLevel);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Success,
                                            HttpStatusCode.OK,
                                            @Resource.DifficultyLevelUpdatedSuccessfully,
                                            transictionLevel);
            }
            else
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.InternalServerError,
                                            "Unable To Update Difficulty Level");
            }
        }

        public async Task<IApiResponse> SoftDeleteTransitionLevelAsync(long id)
        {
            var delta = _commonService._unitOfWork.Repository<TransitionLevel, long>();

            var deltaData = await delta.GetObjAsync(x => x.Id == id);

            if (deltaData != null)
            {
                bool profileUsedInPaper = await _commonService
                    ._unitOfWork
                    .Repository<PaperMetadata, long>()
                    .IsExistAsync(pm => pm.TransitionProfileId == deltaData.TransitionProfileId);

                if (profileUsedInPaper)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.BadRequest,
                        Resource.LevelLinkedToProfile
                    );
                }

                delta.SoftDelete(deltaData);

                await _commonService
                        ._unitOfWork
                        .Complete();

                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.DifficultyLevelHasBeenDeletedSuccessfully,
                                        deltaData);
            }
            else
            {
                return _commonService
                                    ._apiResponse
                                    .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        $"Transition Level with ID {id} not found.");
            }
        }

        public async Task<IApiResponse> GetTransitionLevels()
        {
            var query = await _commonService._unitOfWork.Repository<TransitionLevel, long>().GetAllAsync();

            var mapData = _mapper.Map<List<GetTransitionLevelsDto>>(query);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              "Fetched all transition levels successfully",
                                                              mapData);
        }


        #region Helper Methods

        private long GetCommonDeltaTypeId()
        {
            var commonDeltaType = nameof(DeltaTypes.Common).ToLower();

            return _commonService
                ._unitOfWork
                .Repository<DeltaType, long>()
                .Query()
                .AsNoTracking()
                .First(x => x.Name.ToLower() == commonDeltaType)
                .Id;
        }

        private async Task<bool> IsDifficultyLevelCommonAsync(long difficultyLevelId)
        {
            var commonDeltaTypeId = GetCommonDeltaTypeId();

            var difficultyLevel = await _commonService
                ._unitOfWork
                .Repository<DifficultyLevel, long>()
                .GetObjAsync(d => d.Id == difficultyLevelId);

            return difficultyLevel?.DeltaTypeId == commonDeltaTypeId;
        }

        private async Task<(bool isDuplicate, string message)> CheckDuplicateDifficultyLevelAsync(
           long transitionProfileId,
           long questionCategoryId,
           long difficultyLevelId,
           long? excludeId = null
        )
        {
            var exists = await _commonService
                ._unitOfWork
                .Repository<TransitionLevel, long>()
                .IsExistAsync(tl =>
                    tl.TransitionProfileId == transitionProfileId &&
                    tl.QuestionCategoryId == questionCategoryId &&
                    tl.DifficultyLevelId == difficultyLevelId &&
                    (excludeId == null || tl.Id != excludeId)
                );

            return (exists, exists ? Resource.DifficultyLevelAlreadyExistsForThisCategory : string.Empty);
        }

        private async Task<(bool hasOverlap, string message)> CheckOverlapWithinCategoryAsync(
            long transitionProfileId,
            long questionCategoryId,
            decimal lowerDScore,
            decimal upperDScore,
            long difficultyLevelId,
            long? excludeId = null
        )
        {
            bool isCommonLevel = await IsDifficultyLevelCommonAsync(difficultyLevelId);

            if (isCommonLevel)
                return (false, string.Empty);

            var commonDeltaTypeId = GetCommonDeltaTypeId();

            var hasOverlap = await _commonService
                ._unitOfWork
                .Repository<TransitionLevel, long>()
                .Query()
                .Include(tl => tl.DifficultyLevel)
                .Where(tl =>
                    tl.TransitionProfileId == transitionProfileId &&
                    tl.QuestionCategoryId == questionCategoryId &&
                    (excludeId == null || tl.Id != excludeId) &&
                    tl.DifficultyLevel.DeltaTypeId != commonDeltaTypeId)
                .AnyAsync(tl => tl.UpperDScore >= lowerDScore && tl.LowerDScore <= upperDScore);

            return (hasOverlap, hasOverlap ? Resource.FromTransitionCannotBeInThisRange : string.Empty);
        }

        #endregion
    }
}
