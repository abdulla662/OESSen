using Microsoft.EntityFrameworkCore;
using SharedHelper.General;
using OES.Core.Entities;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QuestionInstructionTemplateService : IQuestionInstructionTemplateService
    {
        private readonly ICommonService _commonService;

        public QuestionInstructionTemplateService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public async Task<ApiResponse> GetPaginatedInstructionTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var repo = _commonService._unitOfWork.Repository<QuestionInstructionTemplate, long>();

            var query = repo.GetAll().AsNoTracking();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey))
            {
                query = query.Where(x => x.Name.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            if (paginationSearchModel.FromDate is not null)
            {
                query = query.Where(x => x.CreationDate >= paginationSearchModel.FromDate && x.CreationDate <= (paginationSearchModel.ToDate ?? DateTimeHelper.Now));
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            if (!paginationSearchModel.PaginationOff)
            {
                var totalItems = await query.CountAsync();

                var data = await query
                    .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                    .Take(paginationSearchModel.PageSize)
                    .ToListAsync();

                var mappedData = _commonService._mapper.Map<List<QuestionInstructionTemplateDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaginationOn,
                    new CustomTableData<QuestionInstructionTemplateDto>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await query.ToListAsync();

                var mappedData = _commonService._mapper.Map<List<QuestionInstructionTemplateDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaginationOff,
                    new CustomTableData<QuestionInstructionTemplateDto>(mappedData, data.Count)
                );
            }
        }

        public async Task<ApiResponse> GetInstructionTemplateAsync(long id)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<QuestionInstructionTemplate, long>()
                .GetByIdAsync(id);

            if (template != null)
            {
                var templateDto = _commonService._mapper.Map<QuestionInstructionTemplateDto>(template);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.TemplateFetchedSuccessfully,
                    templateDto
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.NotFound,
                HttpStatusCode.NotFound,
                Resource.InstructionTemplateNotFound
            );
        }

        public async Task<ApiResponse> SaveInstructionTemplateAsync(QuestionInstructionTemplateDto instructionTemplateDto)
        {
            var repo = _commonService._unitOfWork.Repository<QuestionInstructionTemplate, long>();
            if (string.IsNullOrWhiteSpace(instructionTemplateDto.Name))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.TemplateNameRequired
                );
            }

            var duplicateExists = await repo.IsExistAsync(x => x.Name.Trim().ToLower() == instructionTemplateDto.Name.Trim().ToLower());
            if (duplicateExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.AlreadyExist,
                    HttpStatusCode.Conflict,
                    Resource.TemplateAlreadyExists
                );
            }

            var entity = _commonService._mapper.Map<QuestionInstructionTemplate>(instructionTemplateDto);

            await repo.AddAsync(entity);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.TemplateSavedSuccessfully,
                    instructionTemplateDto
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToSaveTemplate
            );
        }

        public async Task<ApiResponse> DeleteInstructionTemplateAsync(long id)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<QuestionInstructionTemplate, long>()
                .GetByIdAsync(id);

            if (template == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound
                );
            }

            _commonService._unitOfWork.Repository<QuestionInstructionTemplate, long>().SoftDelete(template);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.TemplateDeletedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailedToDeleteTemplate
            );
        }
    }
}
