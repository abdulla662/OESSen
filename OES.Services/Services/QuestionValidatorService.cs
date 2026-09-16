using OES.Core.Entities;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QuestionValidatorService : IQuestionValidatorService
    {
        private readonly ICommonService _commonService;

        public QuestionValidatorService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public ApiResponse ValidateAddedOrUpdatedQuestionMetadata(QuestionMetadataAdditionOrUpdateDto dto)
        {
            if (dto == null ||
                dto.QuestionTypeId == 0 ||
                dto.QuestionSubjectId == 0 ||
                dto.DifficultyProfileId == 0 ||
                dto.DifficultyLevelId == 0 ||
                !dto.IsRoot ||
                string.IsNullOrWhiteSpace(dto.Author)
            )
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.PleaseEnterValidFieldData);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse();
            }
        }

        public async Task<ApiResponse> ValidateNewLanguageDetails(QuestionDetailsDto dto)
        {
            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(x => x.Id == dto.QuestionMetadataId);

            bool isPaperAnswered = questionMetadata?.QuestionTypeId == (long)OES.Helper.Enums.QuestionType.PaperAnsweredQuestion;

            if (dto == null ||
                string.IsNullOrEmpty(dto.Body) ||
                (!isPaperAnswered && string.IsNullOrEmpty(dto.ModelAnswer)) ||
                dto.QuestionMetadataId == 0 ||
                dto.LanguageId == 0
            )
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.PleaseEnterValidFieldData);
            }
            else if (await _commonService
                     ._unitOfWork
                     .Repository<QuestionDetails, long>()
                     .IsExistAsync(x => x.LanguageId == dto.LanguageId && x.QuestionMetadataId == dto.QuestionMetadataId && !x.IsDeleted)
            )
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.QuestionLanguageAlreadyAssigned);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse();
            }
        }
    }
}
