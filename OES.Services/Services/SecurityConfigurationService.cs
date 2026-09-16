using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class SecurityConfigurationService : ISecurityConfigurationService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;
        private readonly IAutoSyncSchedulesService _autoSyncSchedulesService;

        public SecurityConfigurationService(ICommonService commonService, IMapper mapper, IAutoSyncSchedulesService autoSyncSchedulesService)
        {
            _commonService = commonService;
            _mapper = mapper;
            _autoSyncSchedulesService = autoSyncSchedulesService;
        }

        public async Task<IApiResponse> GetAllSecurityConfigurationsTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfigurationTemplate, long>()
                .GetAll()
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                var searchKeyLower = paginationSearchModel.SearchKey.ToLower();

                query = query.Where(a => a.Name.ToLower().Contains(searchKeyLower));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoTemplatesFound);

            var paginatedTemplates = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (paginatedTemplates.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoTemplatesFound);
            }

            var templateDtos = _commonService._mapper.Map<List<SecurityConfigurationTemplateDto>>(paginatedTemplates);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.TemplateRetrievedSuccessfully,
                                new CustomTableData<SecurityConfigurationTemplateDto>(templateDtos, totalRecords));
        }

        public async Task<IApiResponse> GetSecurityConfigurationsTemplateByIdAsync(long securityTemplateId)
        {
            var template = await _commonService._unitOfWork.Repository<ScheduleSecurityConfigurationTemplate, long>().GetByIdAsync(securityTemplateId);

            if (template == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateRetrievedSuccessfully);

            var deserializedData = JsonSerializer.Deserialize<ScheduleSecConfigResponseDto>(template.Data);

            if (deserializedData == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.InternalServerError, Resource.InvalidDataFormat);

            var scheduleSecConfigResponseDto = new ScheduleSecConfigResponseDto()
            {
                Id = template.Id,
                TemplateName = template.Name,
                SecuredBrowser = deserializedData.SecuredBrowser,
                EnableLog = deserializedData.EnableLog,
                DisplayLog = deserializedData.DisplayLog,
                ScreenShots = deserializedData.ScreenShots,
                CamShot = deserializedData.CamShot,
                CandidateCameraAndCameraShots = deserializedData.CandidateCameraAndCameraShots,
                EvidenceDurationPerSeconds = deserializedData.EvidenceDurationPerSeconds,
                SuperVisorAndCandidateCameraShot = deserializedData.SuperVisorAndCandidateCameraShot,
                SuperVisorAndCandidateCombinedCameraShot = deserializedData.SuperVisorAndCandidateCombinedCameraShot,
                ScreenShotsOnSubmitOrSkip = deserializedData.ScreenShotsOnSubmitOrSkip,
                SystemWarningWhenNoFace = deserializedData.SystemWarningWhenNoFace,
                AudioRecording = deserializedData.AudioRecording,
                FocusOutWindowsMinimizedScreenShot = deserializedData.FocusOutWindowsMinimizedScreenShot,
                FocusOutWindowsMinimizedCameraShot = deserializedData.FocusOutWindowsMinimizedCameraShot,
                Proctoring = deserializedData.Proctoring,
                ProctoringWarning = deserializedData.ProctoringWarning,
                IsCandidateIdCardRequired = deserializedData.IsCandidateIdCardRequired,
                SupportedVersionForSecuredBrowser = deserializedData.SupportedVersionForSecuredBrowser,
                CloseApplicationPasswordRequired = deserializedData.CloseApplicationPasswordRequired,
                RestrictAccessRequire = deserializedData.RestrictAccessRequire,
                IsOnLineExam = deserializedData.IsOnLineExam,
                IsAutoSyncEnabled = deserializedData.IsAutoSyncEnabled,
                SyncScheduleTime = deserializedData.SyncScheduleTime
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.TemplateRetrievedSuccessfully, scheduleSecConfigResponseDto);
        }

        public async Task<IApiResponse> GetSecurityConfigurationsByScheduleIdAsync(long scheduleId)
        {
            if (scheduleId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidScheduleId
                );
            }

            var scheduleExists = await _commonService._unitOfWork.Repository<ScheduleMetadata, long>().IsExistAsync(e => e.Id == scheduleId);

            if (!scheduleExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            var securityConfigurations = await _commonService._unitOfWork.Repository<ScheduleSecurityConfiguration, long>().GetObjAsync(e => e.ScheduleMetadataId == scheduleId);

            if (securityConfigurations == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.NotFound,
                    Resource.SecurityConfigurationsNotFoundForThisSchedule
                );
            }

            var mappedConfigurations = _mapper.Map<ExamSecurityConfigurationDto>(securityConfigurations);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SecurityConfigurationsRetrievedSuccessfully,
                mappedConfigurations
            );
        }

        public async Task<IApiResponse> AddSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto addSecurityConfigurationDto)
        {
            if (addSecurityConfigurationDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.InvalidDataProvided);
            }

            var securityConfigurations = await _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfiguration, long>()
                .GetObjAsync(x => x.ScheduleMetadataId == addSecurityConfigurationDto.ScheduleId);

            if (securityConfigurations != null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.SecurityConfigurationsAlreadyExistForThisSchedule);
            }

            var scheduleMetadata = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetByIdAsync(addSecurityConfigurationDto.ScheduleId);

            if (scheduleMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.InternalServerError,
                                                                  HttpStatusCode.InternalServerError,
                                                                  Resource.SpecifiedScheduleDoesNotExist);
            }

            var newMappedConfigurations = _mapper.Map<ScheduleSecurityConfiguration>(addSecurityConfigurationDto);

            await _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfiguration, long>()
                .AddAsync(newMappedConfigurations);

            ApplySyncSettings(scheduleMetadata, addSecurityConfigurationDto);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.SecurityConfigurationAddedSuccefully,
                                                                  newMappedConfigurations);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.FailedToAddSecurityConfigurations);
            }
        }

        public async Task<IApiResponse> UpdateSecurityConfigurationsAsync(AddOrUpdateSecurityConfigurationDto updateSecurityConfigurationDto)
        {
            if (updateSecurityConfigurationDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidDataProvided
                );
            }

            var securityConfigurations = await _commonService
               ._unitOfWork
               .Repository<ScheduleSecurityConfiguration, long>()
               .GetObjAsync(e => e.ScheduleMetadataId == updateSecurityConfigurationDto.ScheduleId);

            if (securityConfigurations == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                   CustomCodeStatus.Failure,
                   HttpStatusCode.BadRequest,
                   Resource.SecurityConfigurationsNotFoundForThisSchedule
               );
            }

            securityConfigurations.SecuredBrowser = updateSecurityConfigurationDto.SecuredBrowser;
            securityConfigurations.EnableLog = updateSecurityConfigurationDto.EnableLog;
            securityConfigurations.DisplayLog = updateSecurityConfigurationDto.DisplayLog;
            securityConfigurations.EvidenceDurationPerSeconds = updateSecurityConfigurationDto.EvidenceDurationPerSeconds;
            securityConfigurations.SupportedVersionForSecuredBrowser = updateSecurityConfigurationDto.SupportedVersionForSecuredBrowser;
            securityConfigurations.ScreenShots = updateSecurityConfigurationDto.ScreenShots;
            securityConfigurations.CamShot = updateSecurityConfigurationDto.CamShot;
            securityConfigurations.CandidateCameraAndCameraShots = updateSecurityConfigurationDto.CandidateCameraAndCameraShots;
            securityConfigurations.SuperVisorAndCandidateCameraShot = updateSecurityConfigurationDto.SuperVisorAndCandidateCameraShot;
            securityConfigurations.SuperVisorAndCandidateCombinedCameraShot = updateSecurityConfigurationDto.SuperVisorAndCandidateCombinedCameraShot;
            securityConfigurations.ScreenShotsOnSubmitOrSkip = updateSecurityConfigurationDto.ScreenShotsOnSubmitOrSkip;
            securityConfigurations.SystemWarningWhenNoFace = updateSecurityConfigurationDto.SystemWarningWhenNoFace;
            securityConfigurations.AudioRecording = updateSecurityConfigurationDto.AudioRecording;
            securityConfigurations.FocusOutWindowsMinimizedScreenShot = updateSecurityConfigurationDto.FocusOutWindowsMinimizedScreenShot;
            securityConfigurations.FocusOutWindowsMinimizedCameraShot = updateSecurityConfigurationDto.FocusOutWindowsMinimizedCameraShot;
            securityConfigurations.Proctoring = updateSecurityConfigurationDto.Proctoring;
            securityConfigurations.ProctoringWarning = updateSecurityConfigurationDto.ProctoringWarning;
            securityConfigurations.IsCandidateIdCardRequired = updateSecurityConfigurationDto.IsCandidateIdCardRequired;
            securityConfigurations.CloseApplicationPasswordRequired = updateSecurityConfigurationDto.CloseApplicationPasswordRequired;
            securityConfigurations.RestrictAccessRequire = updateSecurityConfigurationDto.RestrictAccessRequire;
            securityConfigurations.IsOnLineExam = updateSecurityConfigurationDto.IsOnLineExam;

            var scheduleMetadata = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetByIdAsync(updateSecurityConfigurationDto.ScheduleId);

            if (scheduleMetadata != null)
                ApplySyncSettings(scheduleMetadata, updateSecurityConfigurationDto);

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.Success,
                                            HttpStatusCode.OK,
                                            Resource.SecurityConfigurationUpdatedSuccefully);
            }
            else
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.InternalServerError,
                                        HttpStatusCode.InternalServerError,
                                        Resource.SomethingWentWrongFailedToUpdateSecurityConfigurations);
            }
        }

        public async Task<IApiResponse> AddSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto configTempRequestDto)
        {
            var similarNameExists = await _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfigurationTemplate, long>()
                .IsExistAsync(q => q.Name.ToLower() == configTempRequestDto.Name.ToLower());

            if (similarNameExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.Conflict,
                                    Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
            }

            var _object = JsonSerializer.Serialize(configTempRequestDto);
            var jObject = JObject.Parse(_object);

            ScheduleSecurityConfigurationTemplate SecConfigTemp = new()
            {
                Name = configTempRequestDto.Name,
                Data = jObject.ToString()
            };

            await _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfigurationTemplate, long>()
                .AddAsync(SecConfigTemp);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Templatehasbeenaddedsuccessfully,
                                    SecConfigTemp.Id);
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

        public async Task<IApiResponse> EditSecurityConfigurationsTemplateAsync(ScheduleSecurityConfigTempRequestDto scheduleSecurityConfigTempRequestDto)
        {
            var similarNameExists = await _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfigurationTemplate, long>()
                .IsExistAsync(q => q.Name.ToLower() == scheduleSecurityConfigTempRequestDto.Name.ToLower() && q.Id != scheduleSecurityConfigTempRequestDto.Id);

            if (similarNameExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.Conflict,
                                    Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
            }

            var oldTemplate = await _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfigurationTemplate, long>()
                .GetObjAsync(q => q.Id == scheduleSecurityConfigTempRequestDto.Id);

            if (oldTemplate == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.Conflict,
                                    "Template not found");
            }

            var _object = JsonSerializer.Serialize(scheduleSecurityConfigTempRequestDto);
            var jObject = JObject.Parse(_object);
            oldTemplate.Data = jObject.ToString();
            oldTemplate.Name = scheduleSecurityConfigTempRequestDto.Name;

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    "Template updated successfully");
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

        public async Task<IApiResponse> DeleteSecurityConfigurationsTemplateAsync(long templateId)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<ScheduleSecurityConfigurationTemplate, long>()
                .GetObjAsync(e => e.Id == templateId);

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
                .Repository<ScheduleSecurityConfigurationTemplate, long>()
                .SoftDelete(template);

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
        private void ApplySyncSettings(ScheduleMetadata scheduleMetadata, AddOrUpdateSecurityConfigurationDto dto)
        {
            scheduleMetadata.IsAutoSyncEnabled = dto.IsAutoSyncEnabled;

            scheduleMetadata.SyncScheduleTime = dto.SyncScheduleTime;

            _commonService._unitOfWork.Repository<ScheduleMetadata, long>().UpdateWithTracking(scheduleMetadata);
        }
        #endregion
    }
}