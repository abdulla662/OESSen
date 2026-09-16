using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Template.Request;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;


        public TemplateService(ICommonService commonService, IMapper mapper)
        {
            _commonService = commonService;
            _mapper = mapper;
        }


        public async Task<IApiResponse> GetAllTemplates(PaginationSearchModel paginationSearchModel)
        {
            var fetchedTemplates = _commonService
                ._unitOfWork
                .Repository<Template, long>()
                .Query()
                .AsNoTracking()
                .Include(tt => tt.TemplateType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey))
            {
                if (paginationSearchModel.SearchInName && paginationSearchModel.SearchInDescription)
                {
                    fetchedTemplates = fetchedTemplates.Where(n => n.Name.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()) || n.Content.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
                }
                else if (paginationSearchModel.SearchInName)
                {
                    fetchedTemplates = fetchedTemplates.Where(n => n.Name.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
                }
                else if (paginationSearchModel.SearchInDescription)
                {
                    fetchedTemplates = fetchedTemplates.Where(n => n.Content.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
                }
            }

            if (paginationSearchModel.FromDate != null)
            {
                fetchedTemplates = fetchedTemplates.Where(o => o.CreationDate >= paginationSearchModel.FromDate &&
                                                               o.CreationDate <= (paginationSearchModel.ToDate ?? DateTimeHelper.Now.Date));
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                var filter = jsonElement.Deserialize<TemplateFilterPaginationModel>();

                if (filter != null && filter._selectedTemplateType > 0)
                {
                    fetchedTemplates = fetchedTemplates.Where(x => x.TemplateTypeId == filter._selectedTemplateType);
                }
            }

            fetchedTemplates = paginationSearchModel.OrderBy == SearchInKey.DESC
               ? fetchedTemplates.OrderByDescending(x => x.CreationDate)
               : fetchedTemplates.OrderBy(x => x.CreationDate);

            var countItems = await fetchedTemplates.CountAsync();

            var data = await fetchedTemplates.Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                                             .Take(paginationSearchModel.PageSize)
                                             .ToListAsync();

            var templateMapping = _mapper.Map<List<TemplateDataDto>>(data);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              null!,
                                                              new CustomTableData<TemplateDataDto>(templateMapping, countItems));
        }


        public async Task<IApiResponse> GetAllListedTemplatesAsync()
        {
            var templates = await _commonService
                ._unitOfWork
                .Repository<Template, long>()
                .GetAllAsync();

            var templatesDtos = _mapper.Map<List<GetListedTemplateResponseDto>>(templates) ?? [];

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                templatesDtos);
        }


        public async Task<IApiResponse> GetTemplateTypes()
        {
            var templateTypes = await _commonService
                ._unitOfWork
            .Repository<TemplateType, long>()
            .GetAllAsync();

            var templateTypesMapping = _mapper.Map<List<TemplateTypeDto>>(templateTypes);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                templateTypesMapping);
        }


        public async Task<IApiResponse> GetTemplateDataById(long id)
        {
            var template = await _commonService
               ._unitOfWork
               .Repository<Template, long>()
               .GetObjAsync(x => x.Id == id, Including: $"{nameof(Template.TemplateType)}");

            if (template != null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Successfully, _commonService._mapper.Map<TemplateDataDto>(template));
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.TemplateNotFound);
        }


        public async Task<IApiResponse> GetTemplateAttributesById(long templateTypeId)
        {
            var templatesAttributes = await _commonService
             ._unitOfWork
             .Repository<TemplateAttribute, long>()
             .GetAllAsync(x => x.TemplateTypeId == templateTypeId, Including: $"{nameof(TemplateAttribute.Attribute)}");

            if (templatesAttributes != null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Successfully, new TemplateAttributesDto
                                    {
                                        Attributes = templatesAttributes.Select(x => x.Attribute.Name).ToList()
                                    });
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.TemplateHasNoAttributes);
            }
        }


        public async Task<ApiResponse> GetTemplatesByTypeId(long templateTypeId)
        {
            var templatesData = await _commonService
             ._unitOfWork
             .Repository<Template, long>()
             .GetAllAsync(x => x.TemplateTypeId == templateTypeId);

            var data = templatesData.Select(x => new GetListedTemplateResponseDto
            {
                Id = x.Id,
                Name = x.Name,
                TemplateTypeId = x.TemplateTypeId
            }).ToList();

            if (data.Count != 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Successfully,
                                    data);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.TypeHasNoTemplates);
            }

        }


        public async Task<IApiResponse> DeleteTemplate(long id)
        {
            var template = await _commonService
               ._unitOfWork
               .Repository<Template, long>()
               .GetObjAsync(x => x.Id == id);

            if (template != null)
            {
                var isUsedInSchedulePaperSettings = await _commonService
                    ._unitOfWork
                    .Repository<SchedulePaperSettings, long>()
                    .IsExistAsync(x =>
                        x.InstructionTemplateId == id ||
                        x.OtherInstructionTemplateId == id ||
                        x.DisclaimerTemplateId == id ||
                        x.RuleTemplateId == id ||
                        x.ResultTemplateId == id ||
                        x.CertificateTemplateId == id
                    );

                if (isUsedInSchedulePaperSettings)
                {
                    return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Failure,
                                            HttpStatusCode.BadRequest,
                                            Resource.TemplateUsedInSchedulePaperSettings);
                }

                _commonService
                    ._unitOfWork
                    .Repository<Template, long>()
                    .SoftDelete(template);

                await _commonService
                    ._unitOfWork
                    .Complete();

                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.TemplateDeletedSuccessfully);
            }
            else
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        string.Format(Resource.TemplateWithIDNotFound, id));
            }
        }


        public async Task<IApiResponse> AddTemplate(AddTemplateRequestDto addTemplateRequestDto)
        {
            var query = await _commonService
                ._unitOfWork
                .Repository<Template, long>()
                .GetObjAsync(x => x.Name.ToLower() == addTemplateRequestDto.Name.ToLower() &&
                                  x.TemplateTypeId == addTemplateRequestDto.TemplateTypeId);

            if (query != null)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.BadRequest,
                                            Resource.TemplateWithTheSameNameAlreadyExists);
            }

            var mappedData = new Template
            {
                TemplateTypeId = addTemplateRequestDto.TemplateTypeId,
                Content = addTemplateRequestDto.Content,
                Name = addTemplateRequestDto.Name
            };

            await _commonService._unitOfWork.Repository<Template, long>().AddAsync(mappedData);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Success,
                                            HttpStatusCode.OK,
                                            Resource.Templatehasbeenaddedsuccessfully);
            }
            else
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.InternalServerError,
                                            Resource.FailedToSaveTemplate);
            }
        }


        public async Task<IApiResponse> UpdateTemplate(UpdateTemplateDto updateTemplateDto)
        {
            var temp = await _commonService
                ._unitOfWork
                .Repository<Template, long>()
                .GetObjAsync(e => e.Id == updateTemplateDto.Id);

            if (updateTemplateDto.Name != temp.Name || updateTemplateDto.TemplateTypeId != temp.TemplateTypeId)
            {
                var IsNameExists = await _commonService._unitOfWork.Repository<Template, long>()
                    .IsExistAsync(n => n.Name.ToLower() == updateTemplateDto.Name.ToLower() &&
                                       n.TemplateTypeId == updateTemplateDto.TemplateTypeId);

                if (IsNameExists)
                {
                    return _commonService
                                ._apiResponse
                                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                HttpStatusCode.BadRequest,
                                                Resource.TemplateWithTheSameNameAlreadyExists);
                }
            }

            _mapper.Map(updateTemplateDto, temp);

            _commonService._unitOfWork.Repository<Template, long>().Update(temp);

            if (await _commonService._unitOfWork.Complete() > 0)
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.TemplateUpdatedSuccessfully,
                                        temp);
            else
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.FailedToUpdateTemplate);
        }
    }
}
