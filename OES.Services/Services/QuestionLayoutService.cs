using AutoMapper;
using OES.Core.Entities;
using OES.Helper.Dtos.QuestionLayout;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QuestionLayoutService(ICommonService _commonService, IMapper _mapper) : IQuestionLayoutService
    {
        public async Task<ApiResponse> GetAllLayoutsByQuestionTypeId(long questionTypeId)
        {
            var layouts = await _commonService
                ._unitOfWork
                .Repository<QuestionLayout, long>()
                .GetAllAsync(result => result.QuestionTypeId == questionTypeId);

            if (layouts == null || !layouts.Any())
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.LayoutNotFounded,
                                    HttpStatusCode.NotFound,
                                    "No layouts found for the provided question type Id");
            }

            var layoutDtos = _mapper.Map<List<LayoutDto>>(layouts);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                "Layouts retrieved successfully",
                                layoutDtos);
        }

        public async Task<Dictionary<long, LayoutDto>> GetDefaultLayoutIdsByQuestionTypeIdsAsync(IEnumerable<long> questionTypeIds)
        {
            var typeIdList = questionTypeIds.ToList();

            if (typeIdList.Count == 0)
                return [];

            var layouts = await _commonService
                ._unitOfWork
                .Repository<QuestionLayout, long>()
                .GetAllAsync(layout => typeIdList.Contains(layout.QuestionTypeId));

            return layouts
                .GroupBy(l => l.QuestionTypeId)
                .ToDictionary(g => g.Key, g => _mapper.Map<LayoutDto>(g.First()));
        }

        public async Task<ApiResponse> AssignLayoutToQuestionMetadataAsync(long questionMetadataId, long layoutId)
        {
            var questionMetadataEntity = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetByIdAsync(questionMetadataId);

            if (questionMetadataEntity is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    "No question metadata found for the given id");
            }

            if (questionMetadataEntity.QuestionLayoutId == layoutId)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.QuestionAssignedToLayout);
            }

            questionMetadataEntity.QuestionLayoutId = layoutId;

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.QuestionAssignedToLayout);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.QuestionDoesNotAssignedToLayout);
        }
    }
}
