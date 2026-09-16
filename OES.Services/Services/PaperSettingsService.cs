using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.Dtos.PaperSetting.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class PaperSettingsService : IPaperSettingsService
    {
        private readonly ICommonService _commonService;

        public PaperSettingsService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public async Task<ApiResponse> GetAllPaperSettingsTemplatesPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<PaperSettingTemplate, long>()
                .GetAll()
                .AsNoTracking();

            List<PaperSettingTemplate> data;

            if (!paginationSearchModel.PaginationOff)
            {
                if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
                    query = query.Where(x => x.Name.Contains(paginationSearchModel.SearchKey));

                if (paginationSearchModel.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= paginationSearchModel.FromDate &&
                                        o.CreationDate < (paginationSearchModel.ToDate ?? DateTime.Today).AddDays(1));
                }

                query = paginationSearchModel.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

                data = await query
                    .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                    .Take(paginationSearchModel.PageSize)
                    .ToListAsync();
            }
            else
            {
                data = await (paginationSearchModel.OrderBy == SearchInKey.DESC ?
                    query.OrderByDescending(x => x.CreationDate).ToListAsync() :
                    query.OrderBy(x => x.CreationDate).ToListAsync());

            }

            int totalItems = data.Count;

            var mappedData = _commonService._mapper.Map<List<PaperSettingsTemplatePaginated>>(data);

            if (mappedData is null || mappedData.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.FailedToLoadSchedulePaperSettingsTemplate);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              Resource.SuccessfullyLoadSchedulePaperSettingsTemplate,
                                                              new CustomTableData<PaperSettingsTemplatePaginated>(mappedData, totalItems));
        }

        public async Task<ApiResponse> GetPaperSettingsTemplateByIdAsync(long id)
        {
            var data = await _commonService
                ._unitOfWork
                .Repository<PaperSettingTemplate, long>()
                .GetObjAsync(e => e.Id == id);

            if (data == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.TemplateNotFound);
            }

            var deserializedData = JsonConvert.DeserializeObject<PaperSettingsTemplateFlattenedDataDto>(data.Data);

            deserializedData.Id = data.Id;
            deserializedData.Name = data.Name;

            var mappedDto = _commonService._mapper.Map<PaperSettingsTemplateRequestDto>(deserializedData);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.TemplateRetrievedSuccessfully,
                                mappedDto);
        }

        public async Task<ApiResponse> GetPaperSettingsBySchedulePaperIdAsync(long schedulePaperId)
        {
            var paperSettings = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperSettings, long>()
                .GetObjAsync(e => e.SchedulePaperId == schedulePaperId);

            if (paperSettings == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperSettingsNotFound
                );
            }

            var paperSettingsDto = _commonService._mapper.Map<GetPaperSettingsResponseDto>(paperSettings);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperSettingRetrivedSuccefully,
                paperSettingsDto
            );
        }

        public async Task<ApiResponse> AddPaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto addPaperSettingsRequestDto)
        {
            var validationResult = ValidatePaperSettings(addPaperSettingsRequestDto);

            if (!validationResult.IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    validationResult.Message
                );
            }

            var paperSettings = _commonService._mapper.Map<SchedulePaperSettings>(addPaperSettingsRequestDto);

            await _commonService._unitOfWork.Repository<SchedulePaperSettings, long>().AddAsync(paperSettings);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaperSettingsAddedSuccessfully,
                    paperSettings
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.SomethingWentWrong
            );
        }

        public async Task<ApiResponse> UpdatePaperSettingsAsync(AddOrUpdatePaperSettingsRequestDto updatePaperSettingsRequestDto)
        {
            var validationResult = ValidatePaperSettings(updatePaperSettingsRequestDto);

            if (!validationResult.IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    validationResult.Message
                );
            }

            var existingPaperSettings = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperSettings, long>()
                .GetObjAsync(e => e.Id == updatePaperSettingsRequestDto.Id);

            if (existingPaperSettings == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperSettingsNotFound
                );
            }

            _commonService._mapper.Map(updatePaperSettingsRequestDto, existingPaperSettings);

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaperSettingsUpdatedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.SomethingWentWrong
            );
        }

        public async Task<ApiResponse> AddPaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto addPaperSettingsTemplateRequestDto)
        {
            var validatorResult = await ValidateAddedPaperSettingTemplate(addPaperSettingsTemplateRequestDto);

            if (validatorResult.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return validatorResult;
            }
            else
            {
                var isNameExists = await _commonService
                   ._unitOfWork
                   .Repository<PaperSettingTemplate, long>()
                   .IsExistAsync(q => q.Name.ToLower() == addPaperSettingsTemplateRequestDto.Name.ToLower());

                if (isNameExists)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.Conflict,
                                        Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
                }

                var flattened = FlattenObject(addPaperSettingsTemplateRequestDto);
                flattened.Remove(nameof(PaperSettingsTemplateRequestDto.Name));
                flattened.Remove(nameof(PaperSettingsTemplateRequestDto.Id));

                PaperSettingTemplate template = new()
                {
                    Name = addPaperSettingsTemplateRequestDto.Name,
                    Data = flattened.ToString()
                };

                await _commonService
                    ._unitOfWork
                    .Repository<PaperSettingTemplate, long>()
                    .AddAsync(template);

                if (await _commonService._unitOfWork.Complete() > 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.Templatehasbeenaddedsuccessfully,
                                        template);
                }
                else
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                        HttpStatusCode.InternalServerError,
                                        Resource.SomethingWentWrong);
                }
            }
        }

        public async Task<ApiResponse> UpdatePaperSettingsTemplateAsync(PaperSettingsTemplateRequestDto updatePaperSettingsTemplateRequestDto)
        {
            var existingTemplate = await _commonService
                ._unitOfWork
                .Repository<PaperSettingTemplate, long>()
                .GetObjAsync(x => x.Id == updatePaperSettingsTemplateRequestDto.Id);

            if (existingTemplate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound);
            }

            var isNameExists = await _commonService
                ._unitOfWork
                .Repository<PaperSettingTemplate, long>()
                .IsExistAsync(q => q.Name.ToLower() == updatePaperSettingsTemplateRequestDto.Name.ToLower() && q.Id != updatePaperSettingsTemplateRequestDto.Id);

            if (isNameExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.Conflict,
                    Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
            }

            var flattened = FlattenObject(updatePaperSettingsTemplateRequestDto);
            flattened.Remove(nameof(PaperSettingsTemplateRequestDto.Name));
            flattened.Remove(nameof(PaperSettingsTemplateRequestDto.Id));

            existingTemplate.Name = updatePaperSettingsTemplateRequestDto.Name;
            existingTemplate.Data = flattened.ToString();

            _commonService._unitOfWork.Repository<PaperSettingTemplate, long>().Update(existingTemplate);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.TemplateUpdatedSuccessfully,
                    existingTemplate);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.SomethingWentWrong);
            }
        }

        public async Task<ApiResponse> DeletePaperSettingsTemplateAsync(long id)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<PaperSettingTemplate, long>()
                .GetObjAsync(e => e.Id == id);

            if (template == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.TemplateNotFound);
            }

            _commonService
                ._unitOfWork
                .Repository<PaperSettingTemplate, long>()
                .Delete(template);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
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
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.SomethingWentWrong);
            }
        }


        #region Helper Methods

        private static (bool IsValid, string Message) ValidatePaperSettings(AddOrUpdatePaperSettingsRequestDto addOrUpdatePaperSettingsRequestDto)
        {
            if (addOrUpdatePaperSettingsRequestDto.ShowWatermark &&
                addOrUpdatePaperSettingsRequestDto.WaterMark != nameof(WaterMarkOption.CandidateCode) &&
                addOrUpdatePaperSettingsRequestDto.WaterMark != nameof(WaterMarkOption.CandidateId) &&
                string.IsNullOrWhiteSpace(addOrUpdatePaperSettingsRequestDto.WaterMark))
            {
                return (false, Resource.PleaseEnterWatermarkText);
            }

            if (addOrUpdatePaperSettingsRequestDto.PassingConcept && addOrUpdatePaperSettingsRequestDto.MinimumPassingMarks <= 0)
            {
                return (false, Resource.MinimumPassingMarksGreaterThanZero);
            }

            if (addOrUpdatePaperSettingsRequestDto.NegativeMarking && (addOrUpdatePaperSettingsRequestDto.NegativeMarkPercent <= 0 || addOrUpdatePaperSettingsRequestDto.NegativeMarkPercent > 100))
            {
                return (false, Resource.NegativeMarkPercentBetweenZeroAndHundred);
            }

            const int timerAlertMinutesMin = 1;
            const int timerAlertMinutesMax = 20;

            if (addOrUpdatePaperSettingsRequestDto.AdditionalPaperSettings.ApplyTimerAlert &&
                !addOrUpdatePaperSettingsRequestDto.AdditionalPaperSettings.TimerAlertMinutes.IsBetween(timerAlertMinutesMin, timerAlertMinutesMax))
            {
                return (false, string.Format(Resource.TimerAlertMinutesValidationBetween, timerAlertMinutesMin, timerAlertMinutesMax));
            }

            if (addOrUpdatePaperSettingsRequestDto.AdditionalPaperSettings.DisclaimerTemplateId <= 0 ||
                addOrUpdatePaperSettingsRequestDto.AdditionalPaperSettings.InstructionTemplateId <= 0)
            {
                return (false, Resource.PleaseSelectAllPaperTemplates);
            }

            if (addOrUpdatePaperSettingsRequestDto.AdditionalPaperSettings.EnableEndTestButtonAfterTimeSpent < 0)
            {
                return (false, Resource.EndTestTimeMustBeGreaterOrEqualZero);
            }

            return (true, string.Empty);
        }

        public async Task<ApiResponse> ValidateAddedPaperSettingTemplate(PaperSettingsTemplateRequestDto paperSettingsTemplateRequestDto)
        {
            if (paperSettingsTemplateRequestDto == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.PleaseEnterValidFieldData);
            }
            else
            {
                var isNameExists = await _commonService
                   ._unitOfWork
                    .Repository<PaperSettingTemplate, long>()
                   .IsExistAsync(q => q.Name.ToLower() == paperSettingsTemplateRequestDto.Name.ToLower());

                if (isNameExists)
                {
                    return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.AlreadyExist,
                                    HttpStatusCode.OK,
                                    Resource.TemplateWithTheSameNameAlreadyExists);
                }
            }

            return _commonService._apiResponse.GetApiResponse();
        }

        public static JObject FlattenObject(object obj)
        {
            var result = new JObject();

            if (obj == null)
                return result;

            var jObject = JObject.FromObject(obj);

            foreach (var property in jObject.Properties())
            {
                if (property.Value.Type == JTokenType.Object)
                {
                    var nested = FlattenObject(property.Value);

                    result.Merge(nested);
                }
                else
                {
                    result[property.Name] = property.Value;
                }
            }

            return result;
        }

        #endregion
    }
}
