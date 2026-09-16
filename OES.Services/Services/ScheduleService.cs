using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OES.Core.Entities;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Schedule;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using SharedHelper.RolesNames;
using System.Data;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly IAutoPermissionAssignmentService _AutoPermissionAssignmentService;
        private readonly IAutoSyncSchedulesService _autoSyncSchedulesService;

        public ScheduleService(ICommonService commonService, FilterParamsValues filterParamsValues, IAutoPermissionAssignmentService autoPermissionAssignmentService, IAutoSyncSchedulesService autoSyncSchedulesService)
        {
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
            _AutoPermissionAssignmentService = autoPermissionAssignmentService;
            _autoSyncSchedulesService = autoSyncSchedulesService;
        }

        public async Task<ApiResponse> GetAllScheduleAsync(PaginationSearchModel pagination)
        {
            var userEmail = _filterParamsValues.UserEmail?.Trim().ToLower();

            var userGroupIds = _filterParamsValues
                .OesUserGroupsAndRoles
                .ConvertAll(g => g.GroupId);

            var scheduleGroupsRepo = _commonService._unitOfWork.Repository<ScheduleGroups, long>();

            var assignedScheduleIds = await scheduleGroupsRepo
                .GetAll(sg => userGroupIds.Contains(sg.OESGroupId) && !sg.IsDeleted)
                .Select(sg => sg.ScheduleId)
                .Distinct()
                .ToListAsync();

            var query = _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetAll();

            bool isSuperAdmin =
                _filterParamsValues.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin) ||
                _filterParamsValues.SsoUserRoles.Exists(x => x.Name == AdminRoles.Entity_Admin);

            if (!isSuperAdmin)
            {
                query = query.Where(s => assignedScheduleIds.Contains(s.Id) || s.CreationUser.ToLower() == userEmail);
            }

            if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInName)
            {
                var key = pagination.SearchKey.ToLower();

                query = query.Where(a => a.Code.ToLower().Contains(key));
            }

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= pagination.FromDate.Value);
            }

            if (pagination.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= pagination.ToDate.Value);
            }

            if (pagination.FilterObj is JsonElement jsonElement)
            {
                var filter = jsonElement.Deserialize<ScheduleFilterPaginationModel>();

                if (filter != null)
                {
                    if (filter.SelectedStatus > 0)
                    {
                        query = query.Where(q => q.PublishingStatus == filter.SelectedStatus);
                    }

                    if (filter.SelectedLocation > 0)
                    {
                        query = query.Where(q => q.ScheduleLocation == filter.SelectedLocation);
                    }

                    if (filter.SelectedSyncStatus.HasValue)
                    {
                        query = query.Where(q => q.SyncStatus == filter.SelectedSyncStatus.Value);
                    }
                }
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoScheduleFound);
            }

            var paginatedSchedules = await query
                .Skip(pagination.PageIndex * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            if (paginatedSchedules.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoScheduleFound);
            }

            var schedulesDtos = _commonService._mapper.Map<List<ScheduleMetadataPaginationDto>>(paginatedSchedules);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.SchedulesRetrievedSuccessfully,
                                new CustomTableData<ScheduleMetadataPaginationDto>(schedulesDtos, totalRecords));
        }

        public async Task<ApiResponse> GetAllUnexpiredPublishedSchedulesPaginatedAsync(PaginationSearchModel paginationSearchModel)
        {
            var dateTimeNow = DateTimeHelper.Now;
            var dateNow = DateOnly.FromDateTime(dateTimeNow);
            var timeNow = TimeOnly.FromDateTime(dateTimeNow);

            var query = _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetAll(x => x.PublishingStatus == PublishingStatus.Published &&
                             (x.EndDate > dateNow || (x.EndDate == dateNow && x.EndTime >= timeNow)))
                .AsNoTracking();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey))
            {
                var searchKey = paginationSearchModel.SearchKey.ToLower();

                query = query.Where(a => a.Code.ToLower().Contains(searchKey) || a.Name.ToLower().Contains(searchKey));
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                var filter = jsonElement.Deserialize<ScheduleFilterPaginationModel>();

                if (filter != null)
                {
                    if (filter.SelectedStatus > 0)
                    {
                        query = query.Where(q => q.PublishingStatus == filter.SelectedStatus);
                    }

                    if (filter.SelectedLocation > 0)
                    {
                        query = query.Where(q => q.ScheduleLocation == filter.SelectedLocation);
                    }
                }
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoScheduleFound);
            }

            var paginatedSchedules = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (paginatedSchedules.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoScheduleFound);
            }

            var schedulesDtos = _commonService._mapper.Map<List<ScheduleMetadataPaginationDto>>(paginatedSchedules);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.SchedulesRetrievedSuccessfully,
                                new CustomTableData<ScheduleMetadataPaginationDto>(schedulesDtos, totalRecords));
        }

        public async Task<ApiResponse> GetScheduleByIdAsync(long id)
        {
            const string includes =
                $"{nameof(ScheduleMetadata.Languages)}.{nameof(ScheduleLanguage.Language)}," +
                $"{nameof(ScheduleMetadata.Venues)}.{nameof(ScheduleVenue.Venue)}," +
                $"{nameof(ScheduleMetadata.ScheduleGroups)}.{nameof(ScheduleGroups.OESGroup)}";

            var scheduleMetadata = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetObjAsync(result => result.Id == id, includes);

            if (scheduleMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            var scheduleDto = new ScheduleMetadataDto
            {
                Id = scheduleMetadata.Id,
                Name = scheduleMetadata.Name,
                Code = scheduleMetadata.Code,
                Description = scheduleMetadata.Description,
                StartDate = scheduleMetadata.StartDate.ToDateTime(scheduleMetadata.StartTime),
                EndDate = scheduleMetadata.EndDate.ToDateTime(scheduleMetadata.EndTime),
                StartTime = scheduleMetadata.StartTime.ToTimeSpan(),
                EndTime = scheduleMetadata.EndTime.ToTimeSpan(),
                ScheduleLocation = scheduleMetadata.ScheduleLocation,
                PublishingStatus = scheduleMetadata.PublishingStatus,
                LanguageIds = scheduleMetadata.Languages?.Select(l => l.LanguageId).ToList() ?? [],
                ExamVenueIds = scheduleMetadata.Venues?.Select(l => l.VenueId).ToList() ?? [],
                ExamVenueNames = scheduleMetadata.Venues?.Select(l => new VenueSyncData(l.VenueId, l.Venue.Name)).ToList() ?? []
            };

            scheduleDto.OESGroupDtos = [.. scheduleMetadata.ScheduleGroups
                .Where(g => !g.OESGroup.IsDeleted)
                .Select(g => new GetOESGroupDto
                {
                    Id = g.OESGroup.Id,
                    Name = g.OESGroup.Name,
                    AutoCreatedForUser = g.OESGroup.AutoCreatedForUser
                })];

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.Successfully,
                                scheduleDto);
        }

        public async Task<ApiResponse> GetScheduleGroupsAsync(long scheduleId)
        {
            var scheduleGroups = await _commonService._unitOfWork
                .Repository<ScheduleGroups, long>()
                .GetAllAsync(x =>
                     x.ScheduleId == scheduleId &&
                    !x.OESGroup.IsTemplate);

            var dto = new ScheduleGroupsDto();

            scheduleGroups
                .Where(sg => sg.OESGroup != null)
                .ToList()
                .ForEach(sg => dto.GroupsIds.Add(sg.OESGroupId));

            var ownerGroupId = scheduleGroups
                .FirstOrDefault(sg => sg.OESGroup != null && sg.OESGroup.AutoCreatedForUser)
                ?.OESGroupId;

            if (ownerGroupId.HasValue && !dto.GroupsIds.Contains(ownerGroupId.Value))
            {
                dto.GroupsIds.Insert(0, ownerGroupId.Value);
            }

            dto.OwnerGroupId = ownerGroupId;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                dto
            );
        }

        public async Task<ApiResponse> GetScheduleValidationParametersAsync(long scheduleMetadataId)
        {
            var schedulePapersCount = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .Query()
                .AsNoTracking()
                .LongCountAsync(x => x.ScheduleMetadataId == scheduleMetadataId);

            var anySchedulePaperHasCandidates = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .Query()
                .AsNoTracking()
                .Include(x => x.Candidates)
                .AnyAsync(x => x.ScheduleMetadataId == scheduleMetadataId && x.Candidates.Any());

            var scheduleValidationParametersResponseDto = new ScheduleValidationParametersResponseDto(schedulePapersCount, anySchedulePaperHasCandidates);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.SchedulePublishingStatusRetrievedSuccessfully,
                                scheduleValidationParametersResponseDto);
        }

        public async Task<ApiResponse> AddScheduleMetadataAsync(AddScheduleMetadataRequestDto addScheduleMetadataRequestDto)
        {
            var validationResult = await ValidateAddingScheduleMetadataAsync(addScheduleMetadataRequestDto);

            var scheduleNameExists = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .IsExistAsync(s => s.Name.Trim().ToLower() == addScheduleMetadataRequestDto.Name.Trim().ToLower());

            if (scheduleNameExists)
            {
                return _commonService
                      ._apiResponse
                      .GetApiResponse(CustomCodeStatus.Conflict,
                                      HttpStatusCode.Conflict,
                                      Resource.ScheduleNameAlreadyExists);
            }

            if (!validationResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validationResult.Message
                );
            }

            var scheduleMetadata = _commonService._mapper.Map<ScheduleMetadata>(addScheduleMetadataRequestDto);

            scheduleMetadata.PublishingStatus = PublishingStatus.NotPublished;

            foreach (var languageId in addScheduleMetadataRequestDto.LanguageIds)
            {
                scheduleMetadata.Languages.Add(new ScheduleLanguage
                {
                    LanguageId = languageId
                });
            }

            foreach (var venueId in addScheduleMetadataRequestDto.ExamVenueIds)
            {
                scheduleMetadata.Venues.Add(new ScheduleVenue
                {
                    VenueId = venueId
                });
            }

            await _commonService._unitOfWork.Repository<ScheduleMetadata, long>().AddAsync(scheduleMetadata);

            if (await _commonService._unitOfWork.Complete() <= 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.Failedtosaveschedulemetadata);
            }

            _ = Guid.TryParse(_filterParamsValues.UserId, out var parsedUserId);

            var isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin);

            if (!isSuperAdmin)
            {
                await _AutoPermissionAssignmentService.AssignDefaultPermissionsForNewEntityAsync(
                    new AutoPermissionAssignmentRequest
                    {
                        EntityId = scheduleMetadata.Id,
                        EntityName = scheduleMetadata.Name,
                        ResourceType = ResourceType.Schedule,
                        UserId = parsedUserId,
                        AdditionalGroupIds = addScheduleMetadataRequestDto.OESGroupDtos?.Select(x => x.Id).ToList(),
                        EntityGroupType = typeof(ScheduleGroups)
                    });
            }

            var scheduleMetadataResponseDto = new GetScheduleMetadataResponseDto(
                scheduleMetadata.Id,
                scheduleMetadata.Languages.Select(l => l.LanguageId).ToList(),
                scheduleMetadata.Venues.Select(v => v.VenueId).ToList(),
                scheduleMetadata.StartDate,
                scheduleMetadata.EndDate,
                scheduleMetadata.StartTime,
                scheduleMetadata.EndTime,
                [.. scheduleMetadata.ScheduleGroups
                    .Where(g => g.OESGroup != null)
                    .Select(g => new GetOESGroupDto
                    {
                        Id = g.OESGroup.Id,
                        Name = g.OESGroup.Name,
                        AutoCreatedForUser = g.OESGroup.AutoCreatedForUser
                    })]
            );

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.Created,
                                Resource.ScheduleMetadataSavedSuccessfully,
                                scheduleMetadataResponseDto);
        }

        public async Task<ApiResponse> UpdateScheduleMetadataAsync(UpdateScheduleMetadataRequestDto updateScheduleMetadataRequestDto)
        {
            var scheduleNameExists = await _commonService
               ._unitOfWork
               .Repository<ScheduleMetadata, long>()
               .IsExistAsync(s => s.Id != updateScheduleMetadataRequestDto.Id && s.Name.Trim().ToLower() == updateScheduleMetadataRequestDto.Name.Trim().ToLower());

            if (scheduleNameExists)
            {
                return _commonService
                      ._apiResponse
                      .GetApiResponse(CustomCodeStatus.Conflict,
                                      HttpStatusCode.Conflict,
                                      Resource.ScheduleNameAlreadyExists);
            }

            var existingSchedule = await _commonService
                 ._unitOfWork
                 .Repository<ScheduleMetadata, long>()
                 .GetAll(
                     x => x.Id == updateScheduleMetadataRequestDto.Id,
                     Including:
                         $"{nameof(ScheduleMetadata.Languages)}," +
                         $"{nameof(ScheduleMetadata.Venues)}," +
                         $"{nameof(ScheduleMetadata.ScheduleGroups)}.{nameof(ScheduleGroups.OESGroup)}"
                 )
                 .FirstOrDefaultAsync();

            if (existingSchedule == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            var validationResult = await ValidateUpdatingScheduleMetadataAsync(updateScheduleMetadataRequestDto, existingSchedule);

            if (!validationResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validationResult.Message
                );
            }

            _commonService._mapper.Map(updateScheduleMetadataRequestDto, existingSchedule);

            existingSchedule.PublishingStatus = PublishingStatus.NotPublished;

            existingSchedule.Languages.Clear();
            existingSchedule.Venues.Clear();

            foreach (var languageId in updateScheduleMetadataRequestDto.LanguageIds)
            {
                existingSchedule.Languages.Add(new ScheduleLanguage
                {
                    LanguageId = languageId
                });
            }

            existingSchedule.Venues.Clear();

            foreach (var venueId in updateScheduleMetadataRequestDto.ExamVenueIds)
            {
                existingSchedule.Venues.Add(new ScheduleVenue
                {
                    VenueId = venueId
                });
            }

            var scheduleGroupRepo = _commonService._unitOfWork.Repository<ScheduleGroups, long>();
            var oldGroups = existingSchedule.ScheduleGroups.ToList();
            var ownerGroup = oldGroups.FirstOrDefault(g => g.OESGroup?.AutoCreatedForUser == true);

            var requestedGroupIds = updateScheduleMetadataRequestDto.OESGroupDtos?
                .Where(x => !x.AutoCreatedForUser)
                .Select(x => x.Id)
                .ToHashSet() ?? [];

            var toRemove = oldGroups
                .Where(g =>
                    (ownerGroup == null || g.OESGroupId != ownerGroup.OESGroupId) &&
                    !requestedGroupIds.Contains(g.OESGroupId)
                )
                .ToList();

            scheduleGroupRepo.DeleteRange(toRemove);

            var existingIds = oldGroups
                .Where(g => ownerGroup == null || g.OESGroupId != ownerGroup.OESGroupId)
                .Select(g => g.OESGroupId)
                .ToHashSet();

            var toAdd = requestedGroupIds.Except(existingIds);

            foreach (var gid in toAdd)
            {
                await scheduleGroupRepo.AddAsync(new ScheduleGroups
                {
                    ScheduleId = existingSchedule.Id,
                    OESGroupId = gid,
                    CreationUser = _filterParamsValues.UserEmail
                });
            }

            await _commonService._unitOfWork.Complete();

            existingSchedule = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetAll(
                    x => x.Id == updateScheduleMetadataRequestDto.Id,
                    Including:
                        $"{nameof(ScheduleMetadata.Languages)}," +
                        $"{nameof(ScheduleMetadata.Venues)}," +
                        $"{nameof(ScheduleMetadata.ScheduleGroups)}.{nameof(ScheduleGroups.OESGroup)}"
                )
                .FirstOrDefaultAsync();

            await _commonService._unitOfWork.Complete();

            var scheduleMetadataResponseDto = new GetScheduleMetadataResponseDto(
                existingSchedule.Id,
                existingSchedule.Languages.Select(l => l.LanguageId).ToList(),
                existingSchedule.Venues.Select(v => v.VenueId).ToList(),
                existingSchedule.StartDate,
                existingSchedule.EndDate,
                existingSchedule.StartTime,
                existingSchedule.EndTime,
                [.. existingSchedule.ScheduleGroups
                    .Select(g => new GetOESGroupDto
                    {
                        Id = g.OESGroupId,
                        Name = g.OESGroup?.Name,
                        AutoCreatedForUser = g.OESGroup?.AutoCreatedForUser ?? false
                    })]
            );

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Schedulemetadataupdatedsuccessfully,
                scheduleMetadataResponseDto
            );
        }

        public async Task<ApiResponse> GetAllSchedulesCreatedByCurrentUser()
        {
            var currentUser = _filterParamsValues.UserEmail?.Trim().ToLower() ?? "system";

            var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

            var groups = await groupRepo.GetAllAsync(g =>
                !g.IsDeleted &&
                !g.IsTemplate &&
                !g.AutoCreatedForUser &&
                g.GroupResources.Any() &&
                g.GroupResources.All(r =>
                    !r.IsDeleted &&
                    r.ResourceType == ResourceType.Schedule
                ) &&
                (
                    g.CreationUser.ToLower() == currentUser ||
                    g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
                ),
                Including: "GroupResources"
            );

            var groupDtos = groups
                .Select(g => new GetOESGroupDto
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .DistinctBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                groupDtos
            );
        }

        public async Task<ApiResponse> DeleteScheduleAsync(long scheduleId)
        {
            var schedule = await _commonService._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetObjAsync(s => s.Id == scheduleId);

            if (schedule == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound);
            }

            if (schedule.PublishingStatus == PublishingStatus.Published)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.CannotDeletePublishedSchedule
                );
            }

            _commonService._unitOfWork.Repository<ScheduleMetadata, long>().SoftDelete(schedule);

            await _commonService._unitOfWork.Complete();

            // Remove Hangfire job for this schedule if it exists
            _autoSyncSchedulesService.RemoveJobForSchedule(scheduleId);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.ScheduleDeletedSuccessfully
            );
        }

        public async Task<ApiResponse> PublishScheduleAsync(long scheduleMetadataId)
        {
            var scheduleMetadata = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetObjAsync(result => result.Id == scheduleMetadataId);

            if (scheduleMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            if (scheduleMetadata.PublishingStatus == PublishingStatus.Published)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.ScheduleAlreadyPublished
                );
            }

            scheduleMetadata.PublishingStatus = PublishingStatus.Published;

            await _commonService._unitOfWork.Complete();

            if (scheduleMetadata.IsAutoSyncEnabled)
            {
                _autoSyncSchedulesService.RegisterJobForSchedule(
                    scheduleMetadata.Id,
                    scheduleMetadata.OrganizationId,
                    scheduleMetadata.OrganizationSignature,
                    scheduleMetadata.SyncScheduleTime
                );
            }
            else
            {
                _autoSyncSchedulesService.RemoveJobForSchedule(scheduleMetadata.Id);
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SchedulePublishedSuccessfully
            );
        }

        public async Task<ApiResponse> CopyScheduleAsync(long scheduleId)
        {
            var scheduleMetadata = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetObjAsync(result => result.Id == scheduleId,
                             Including: $"{nameof(ScheduleMetadata.Papers)}.{nameof(SchedulePaper.PaperSettings)}," +
                                        $"{nameof(ScheduleMetadata.Papers)}.{nameof(SchedulePaper.Candidates)}," +
                                        $"{nameof(ScheduleMetadata.SecurityConfiguration)}," +
                                        $"{nameof(ScheduleMetadata.Venues)}," +
                                        $"{nameof(ScheduleMetadata.Languages)}");

            if (scheduleMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            var copiedSchedule = _commonService._mapper.Map<ScheduleMetadata>(scheduleMetadata);

            await _commonService._unitOfWork.Repository<ScheduleMetadata, long>().AddAsync(copiedSchedule);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.ScheduleCopiedSuccessfully
            );
        }

        public async Task<ApiResponse> GetAllScheduleTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<ScheduleTemplate, long>()
                .GetAll()
                .AsQueryable();

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                var searchKeyLower = paginationSearchModel.SearchKey.ToLower();

                query = query.Where(a => a.Name.ToLower().Contains(searchKeyLower));
            }

            if (paginationSearchModel.FromDate.HasValue)
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);

            if (paginationSearchModel.ToDate.HasValue)
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);

            if (!query.Any())
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);

            var pageIndex = Math.Max(0, paginationSearchModel.PageIndex);

            var pageSize = Math.Max(1, paginationSearchModel.PageSize);

            var paginatedTemplates = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            if (!paginatedTemplates.Any())
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);

            var templateDtos = paginatedTemplates
                .ConvertAll(q => new ScheduleTemplateDto
                {
                    Id = q.Id,
                    Name = q.Name
                });

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.TemplateFound,
                                new CustomTableData<ScheduleTemplateDto>(templateDtos, totalRecords));
        }

        public async Task<ApiResponse> GetScheduleTemplateByIdAsync(long scheduleTemplateId)
        {
            var template = await _commonService._unitOfWork.Repository<ScheduleTemplate, long>().GetByIdAsync(scheduleTemplateId);

            if (template == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);

            var deserializedData = JsonConvert.DeserializeObject<ScheduleCreationTemplateDto>(template.Data);

            if (deserializedData == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.InternalServerError, Resource.InvalidDataFormat);

            var scheduleMetadataDto = new ScheduleMetadataRetrievalDto
            {
                Id = template.Id,
                Name = deserializedData.Name,
                Code = deserializedData.Code,
                Description = deserializedData.Description,
                StartDate = deserializedData.StartDate,
                StartTime = deserializedData.StartTime,
                EndDate = deserializedData.EndDate,
                EndTime = deserializedData.EndTime,
                ScheduleLocation = deserializedData.ScheduleLocation,
                LanguageIds = deserializedData.LanguageIds,
                ExamVenueIds = deserializedData.ExamVenueIds
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.TemplateFound, scheduleMetadataDto);
        }

        public async Task<ApiResponse> AddScheduleTemplateAsync(ScheduleCreationTemplateDto scheduleTemplateDto)
        {
            var validatorResult = ValidateAddedOrUpdatedScheduleMetadata(scheduleTemplateDto);

            if (!validatorResult.IsValid)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    validatorResult.Message);
            }

            var templateNameExists = await _commonService
                ._unitOfWork
                .Repository<ScheduleTemplate, long>()
                .IsExistAsync(s => s.Name.Trim().ToLower() == scheduleTemplateDto.TemplateName.Trim().ToLower());

            if (templateNameExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.Conflict,
                                    Resource.AtemplatewiththesamenamealreadyexistsPleasechooseadifferentname);
            }

            var _object = System.Text.Json.JsonSerializer.Serialize(scheduleTemplateDto);

            var jObject = JObject.Parse(_object);

            jObject.Remove(nameof(ScheduleCreationTemplateDto.TemplateName));

            var template = new ScheduleTemplate
            {
                Name = scheduleTemplateDto.TemplateName,
                Data = jObject.ToString()
            };

            await _commonService
                ._unitOfWork
                .Repository<ScheduleTemplate, long>()
                .AddAsync(template);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Templatehasbeenaddedsuccessfully,
                                    template.Id);
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

        public async Task<ApiResponse> DeleteScheduleMetadataTemplateAsync(long templateId)
        {
            var template = await _commonService
                ._unitOfWork
                .Repository<ScheduleTemplate, long>()
                .GetObjAsync(x => x.Id == templateId);

            if (template == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.NotFound,
                    Resource.TemplateNotFound
                );
            }

            _commonService._unitOfWork.Repository<ScheduleTemplate, long>().Delete(template);

            if (await _commonService._unitOfWork.Complete() > 0)
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


        #region Helper Methods

        public async Task<(bool IsValid, string Message)> ValidateAddingScheduleMetadataAsync(AddScheduleMetadataRequestDto addScheduleMetadataRequestDto)
        {
            if (string.IsNullOrWhiteSpace(addScheduleMetadataRequestDto.Code))
            {
                return (false, Resource.ScheduleCodeIsRequired);
            }

            var similarScheduleInCode = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetObjAsync(result => result.Code == addScheduleMetadataRequestDto.Code);

            if (similarScheduleInCode != null)
            {
                return (false, Resource.ScheduleCodeAlreadyExists);
            }

            if (string.IsNullOrWhiteSpace(addScheduleMetadataRequestDto.Name))
            {
                return (false, Resource.ScheduleNameIsRequired);
            }

            if (addScheduleMetadataRequestDto.LanguageIds?.Count == 0)
            {
                return (false, Resource.AtLeastOneLanguageMustBeSelected);
            }

            if (addScheduleMetadataRequestDto.ExamVenueIds?.Count == 0)
            {
                return (false, Resource.AtLeastOneVenueMustBeSelected);
            }

            if (addScheduleMetadataRequestDto.EndDate.Date < addScheduleMetadataRequestDto.StartDate.Date)
            {
                return (false, Resource.EndDateMustBeGreaterThanStartDate);
            }

            var startDateTime = addScheduleMetadataRequestDto.StartDate.Date.Add(addScheduleMetadataRequestDto.StartTime);
            var endDateTime = addScheduleMetadataRequestDto.EndDate.Date.Add(addScheduleMetadataRequestDto.EndTime);

            if (endDateTime <= startDateTime)
            {
                return (false, Resource.Enddatetimemustbeafterstartdatetime);
            }

            if (!Enum.IsDefined(typeof(ScheduleLocation), addScheduleMetadataRequestDto.ScheduleLocation))
            {
                return (false, Resource.ValidScheduleLocationMustBeSelected);
            }

            return (true, string.Empty);
        }

        public async Task<(bool IsValid, string Message)> ValidateUpdatingScheduleMetadataAsync(UpdateScheduleMetadataRequestDto updateScheduleMetadataRequestDto, ScheduleMetadata existingSchedule)
        {
            if (existingSchedule == null)
            {
                return (false, Resource.ScheduleNotFound);
            }

            if (string.IsNullOrWhiteSpace(updateScheduleMetadataRequestDto.Code))
            {
                return (false, Resource.ScheduleCodeIsRequired);
            }

            var similarScheduleInCode = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetObjAsync(x => x.Code == updateScheduleMetadataRequestDto.Code && x.Id != updateScheduleMetadataRequestDto.Id);

            if (similarScheduleInCode != null)
            {
                return (false, Resource.ScheduleCodeAlreadyExists);
            }

            if (string.IsNullOrWhiteSpace(updateScheduleMetadataRequestDto.Name))
            {
                return (false, Resource.ScheduleNameIsRequired);
            }

            if (updateScheduleMetadataRequestDto.LanguageIds?.Count == 0)
            {
                return (false, Resource.AtLeastOneLanguageMustBeSelected);
            }

            var scheduleValidationParametersDto = (ScheduleValidationParametersResponseDto)(await GetScheduleValidationParametersAsync(updateScheduleMetadataRequestDto.Id)).Data;

            if (scheduleValidationParametersDto?.SchedulePapersCount > 0)
            {
                var providedLanguagesIds = updateScheduleMetadataRequestDto.LanguageIds;
                var existingLanguagesIds = existingSchedule.Languages.Select(l => l.LanguageId);
                var missingLanguagesIds = existingLanguagesIds.Except(providedLanguagesIds);
                if (missingLanguagesIds.Any())
                {
                    return (false, Resource.CannotUnselectDisabledCheckedLanguages);
                }
            }

            if (scheduleValidationParametersDto?.AnyPaperOnScheduleHasCandidates == true)
            {
                var providedVenuesIds = updateScheduleMetadataRequestDto.ExamVenueIds;
                var existingVenuesIds = existingSchedule.Venues.Select(v => v.VenueId);
                var missingVenuesIds = existingVenuesIds.Except(providedVenuesIds);
                if (missingVenuesIds.Any())
                {
                    return (false, Resource.CannotUnselectDisabledCheckedVenues);
                }
            }

            if (updateScheduleMetadataRequestDto.ExamVenueIds?.Count == 0)
            {
                return (false, Resource.AtLeastOneVenueMustBeSelected);
            }

            if (!(updateScheduleMetadataRequestDto.StartDate.HasValue &&
                  updateScheduleMetadataRequestDto.StartTime.HasValue &&
                  updateScheduleMetadataRequestDto.EndDate.HasValue &&
                  updateScheduleMetadataRequestDto.EndTime.HasValue))
            {
                return (false, Resource.StartEndDateTimeAllRequired);
            }

            if (updateScheduleMetadataRequestDto.EndDate.Value.Date < updateScheduleMetadataRequestDto.StartDate.Value)
            {
                return (false, Resource.EndDateMustBeGreaterThanStartDate);
            }

            var startDateTime = updateScheduleMetadataRequestDto.StartDate.Value.Date.Add(updateScheduleMetadataRequestDto.StartTime.Value);
            var endDateTime = updateScheduleMetadataRequestDto.EndDate.Value.Date.Add(updateScheduleMetadataRequestDto.EndTime.Value);

            if (endDateTime <= startDateTime)
            {
                return (false, Resource.Enddatetimemustbeafterstartdatetime);
            }

            if (!Enum.IsDefined(typeof(ScheduleLocation), updateScheduleMetadataRequestDto.ScheduleLocation))
            {
                return (false, Resource.ValidScheduleLocationMustBeSelected);
            }

            return (true, string.Empty);
        }

        public static (bool IsValid, string Message) ValidateAddedOrUpdatedScheduleMetadata(ScheduleCreationTemplateDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Code) ||
                dto.LanguageIds.Count == 0 ||
                dto.ExamVenueIds.Count == 0)
            {
                return (false, Resource.PleaseEnterValidFieldData);
            }

            var startDateTime = dto.StartDate.Date.Add(dto.StartTime);
            var endDateTime = dto.EndDate.Date.Add(dto.EndTime);

            if (endDateTime <= startDateTime)
            {
                return (false, Resource.Enddatetimemustbeafterstartdatetime);
            }

            return (true, string.Empty);
        }

        public async Task<ApiResponse> UpdateScheduleSyncStatus(long scheduleId)
        {
            if (scheduleId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.InvalidScheduleId
                );
            }

            var repo = _commonService._unitOfWork.Repository<ScheduleMetadata, long>();

            var scheduleMetadata = await repo.GetByIdAsync(scheduleId);

            if (scheduleMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            if (scheduleMetadata.SyncStatus == SyncingStatus.Synced)
            {
                return _commonService._apiResponse.GetApiResponse(
                     CustomCodeStatus.Success,
                     HttpStatusCode.OK,
                     Resource.ScheduleSyncStatusUpdated
                 );
            }

            scheduleMetadata.SyncStatus = SyncingStatus.Synced;

            repo.UpdateWithTracking(scheduleMetadata);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ScheduleSyncStatusUpdated
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.ScheduleSyncStatusFailed
            );
        }

        public async Task<ApiResponse> ToggleAutoSyncAsync(long scheduleId, bool isEnabled)
        {
            if (scheduleId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.InvalidScheduleId
                );
            }

            var repo = _commonService._unitOfWork.Repository<ScheduleMetadata, long>();

            var scheduleMetadata = await repo.GetByIdAsync(scheduleId);

            if (scheduleMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            scheduleMetadata.IsAutoSyncEnabled = isEnabled;

            repo.UpdateWithTracking(scheduleMetadata);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    isEnabled ? Resource.AutoSyncEnabled : Resource.AutoSyncDisabled
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.SomethingWentWrong
            );
        }

        public async Task<ApiResponse> GetAutoSyncStatusAsync(long scheduleId)
        {
            if (scheduleId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.InvalidScheduleId
                );
            }

            var repo = _commonService._unitOfWork.Repository<ScheduleMetadata, long>();

            var scheduleMetadata = await repo.GetByIdAsync(scheduleId);

            if (scheduleMetadata == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Success,
                new AutoSyncSettingsDto
                {
                    IsAutoSyncEnabled = scheduleMetadata.IsAutoSyncEnabled,
                    SyncScheduleTime = scheduleMetadata.SyncScheduleTime
                }
            );
        }

        #endregion Helper Methods
    }
}