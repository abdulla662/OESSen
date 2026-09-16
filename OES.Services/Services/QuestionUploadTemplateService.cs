using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QuestionUploadTemplateService : IQuestionUploadTemplateService
    {
        private readonly ICommonService _commonService;

        public QuestionUploadTemplateService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public async Task<ApiResponse> GetAllPaginatedTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<QuestionUploadTemplate, long>()
                .Query()
                .AsNoTracking();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey))
            {
                query = query.Where(x => x.Name.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            var totalRecordsCount = await query.CountAsync();

            var list = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            var dtos = _commonService._mapper.Map<List<QuestionUploadTemplateDto>>(list);

            var tableData = new CustomTableData<QuestionUploadTemplateDto>(dtos, totalRecordsCount);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.TemplateFetchedSuccessfully,
                tableData
            );
        }

        public async Task<ApiResponse> GetByIdAsync(long templateId)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<QuestionUploadTemplate, long>()
                .GetObjAsync(x => x.Id == templateId);

            if (template == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.templateNotFound
                );
            }

            var dto = _commonService._mapper.Map<QuestionUploadTemplateDto>(template);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.TemplateFetchedSuccessfully,
                dto
            );
        }

        public async Task<ApiResponse> AddTemplateAsync(QuestionUploadTemplateDto questionUploadTemplateDto)
        {
            var templateExists = await _commonService
                ._unitOfWork
                .Repository<QuestionUploadTemplate, long>()
                .IsExistAsync(x => x.Name == questionUploadTemplateDto.Name);

            if (templateExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Conflict,
                    HttpStatusCode.Conflict,
                    Resource.TemplateWithTheSameNameAlreadyExists
                );
            }

            var entity = _commonService._mapper.Map<QuestionUploadTemplate>(questionUploadTemplateDto);

            await _commonService._unitOfWork.Repository<QuestionUploadTemplate, long>().AddAsync(entity);
            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.TemplateAddedSuccessfully
            );
        }

        public async Task<ApiResponse> DeleteTemplateAsync(long templateId)
        {
            if (templateId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                   CustomCodeStatus.NotFound,
                    HttpStatusCode.BadRequest,
                    Resource.templateNotFound
                );
            }

            var entity = await _commonService
                ._unitOfWork
                .Repository<QuestionUploadTemplate, long>()
                .GetObjAsync(x => x.Id == templateId);

            if (entity == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.templateNotFound
                );
            }

            _commonService._unitOfWork.Repository<QuestionUploadTemplate, long>().SoftDelete(entity);
            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.TemplateDeletedSuccessfully
            );
        }
    }
}
