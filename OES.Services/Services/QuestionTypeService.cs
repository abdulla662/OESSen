using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QuestionTypeService(ICommonService _commonService, IMapper _mapper) : IQuestionTypeService
    {
        public async Task<IApiResponse> GetAllQuestionType(PaginationSearchModel pagination)
        {
            var query = _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().GetAll().AsNoTracking();

            if (!string.IsNullOrEmpty(pagination.SearchKey))
            {
                query = query.Where(q => q.Name.Contains(pagination.SearchKey));
            }

            var totalItems = await query.CountAsync();

            if (!pagination.PaginationOff)
            {
                query = query.Skip(pagination.PageIndex * pagination.PageSize)
                             .Take(pagination.PageSize);
            }

            var questionTypes = await query.ToListAsync();

            var questionTypeDtos = _mapper.Map<List<QuestionTypeDto>>(questionTypes);


            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Question Types Fetched Successfully",
                new CustomTableData<QuestionTypeDto>(questionTypeDtos, totalItems)
            );
        }

        public async Task<ApiResponse> CreateQuestionType(QuestionTypeDto questionTypeDto)
        {
            var questionType = _mapper.Map<Core.Entities.QuestionType>(questionTypeDto);

            await _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().AddAsync(questionType);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var createdDto = _mapper.Map<QuestionTypeDto>(questionType);
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.Created, Resource.QuestionTypeCreatedSuccessfully, createdDto);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.BadRequest, Resource.FailedToCreateQuestionType);

        }

        public async Task<ApiResponse> UpdateQuestionType(QuestionTypeDto questionTypeUpdateDto)
        {
            var questionType = await _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().GetByIdAsync(questionTypeUpdateDto.Id);

            if (questionType == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "Question Type Not Found");

            questionType.Name = questionTypeUpdateDto.Name;

            _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().Update(questionType);
            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var updatedDto = _mapper.Map<QuestionTypeDto>(questionType);
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.QuestionTypeUpdatedSuccessfully, updatedDto);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.BadRequest, Resource.FailedToUpdatedQuestionType);
        }

        public async Task<ApiResponse> DeleteQuestionType(long id)
        {
            var questionType = await _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().GetByIdAsync(id);

            if (questionType == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "Question Type Not Found");

            _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().SoftDelete(questionType);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.QuestionTypeDeletedSuccessfully);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.BadRequest, Resource.FailedToDeleteQuestionType);
        }

        public async Task<ApiResponse> GetQuestionTypeById(long id)
        {
            var questionType = await _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().GetByIdAsync(id);

            if (questionType == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "Question Type Not Found");

            var dto = _mapper.Map<QuestionTypeDto>(questionType);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, "Question Type Retrieved Successfully", dto);
        }

        public async Task<ApiResponse> GetAllQuestionTypes()
        {
            var questionTypes = await _commonService._unitOfWork.Repository<Core.Entities.QuestionType, long>().GetAllAsync(x => x.IsActive && !x.IsDeleted);

            if (questionTypes == null || !questionTypes.Any())
                _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NoQuestionTypesFound, HttpStatusCode.NotFound, "No Question Types found");

            var dtoList = _mapper.Map<List<QuestionTypeDto>>(questionTypes);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, "Question Types retrieved successfully", dtoList);
        }
    }
}
