using AutoMapper;
using OES.Core.Entities;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly INotificationHubService _notificationHubService;

        public NotificationService(ICommonService commonService, IMapper mapper, FilterParamsValues filterParamsValues, INotificationHubService notificationHubService)
        {
            _commonService = commonService;
            _mapper = mapper;
            _filterParamsValues = filterParamsValues;
            _notificationHubService = notificationHubService;
        }

        public async Task<ApiResponse> GetAllNotificationPaginated(PaginationSearchModel searchModel)
        {
            var appUser = await _commonService
             ._unitOfWork
             .Repository<AppUserProfile, Guid>()
             .GetObjAsync(x => x.EmailAddress == _filterParamsValues.UserEmail);

            if (appUser == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.UserNotFound
                );
            }

            var query = await _commonService
                ._unitOfWork
                .Repository<NotificationAppUserProfile, long>()
                .GetAllAsync(
                    x => x.AppUserProfileId == appUser.Id,
                    Including: "Notification",
                    asNoTracking: true);

            if (!string.IsNullOrEmpty(searchModel.SearchKey))
            {
                if (searchModel.SearchInName)
                {
                    query = query.Where(a => Resource.NotificationMessage(a.Notification.Entity, a.Notification.Operation, a.Notification.Status, a.Notification.ParameterName).ToLower().Contains(searchModel.SearchKey.ToLower()));
                }

                if (searchModel.SearchInDescription)
                {
                    query = query.Where(a => Resource.NotificationMessage(a.Notification.Entity, a.Notification.Operation, a.Notification.Status, a.Notification.ParameterName).ToLower().Contains(searchModel.SearchKey.ToLower()));
                }
            }
            if (searchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= searchModel.FromDate.Value);
            }

            if (searchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= searchModel.ToDate.Value);
            }

            query = searchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = query.Count();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoNotificationsFound);
            }

            var paginatedAssignments = query
                .Skip(searchModel.PageIndex * searchModel.PageSize)
                .Take(searchModel.PageSize);

            if (!paginatedAssignments.Any())
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoMoreNotificationsFound);
            }

            List<CustomNotificationDto> notificationDtoList = new List<CustomNotificationDto>();

            foreach (var q in paginatedAssignments)
            {
                var notificationDto = new CustomNotificationDto
                {
                    NotificationId = q.NotificationId,
                    Entity = q.Notification.Entity,
                    Operation = q.Notification.Operation,
                    AffectedRows = q.Notification.AffectedRows,
                    Status = q.Notification.Status,
                    Type = q.Notification.Type,
                    From = q.Notification.From,
                    IsRead = q.IsRead,
                };

                notificationDtoList.Add(notificationDto);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.ActiveNotificationsFound, new CustomTableData<CustomNotificationDto>(notificationDtoList.ToList(), totalRecords));
        }

        public async Task<ApiResponse> GetNotificationById(long id)
        {
            var query = await _commonService
                ._unitOfWork.Repository<NotificationAppUserProfile, long>().GetObjAsync(e => e.NotificationId == id, Including: "Notification");

            if (query != null)
            {
                query.IsRead = true;
                await _commonService
                ._unitOfWork.Complete();
            }

            var notificationDto = _mapper.Map<NotificationAppUserProfileDto>(query);

            if (notificationDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.NotificationIsNotFound, System.Net.HttpStatusCode.NotFound, Resource.NotificationNotFound);
            }

            return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.Success, System.Net.HttpStatusCode.OK, null, notificationDto);
        }

        public async Task<ApiResponse> GetFilteredNotification(Guid id)
        {
            var appUser = await _commonService
              ._unitOfWork
              .Repository<AppUserProfile, Guid>()
              .GetObjAsync(x => x.EmailAddress == _filterParamsValues.UserEmail);

            if (appUser == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotificationIsNotFound,
                    HttpStatusCode.NotFound,
                    Resource.UserNotFound);
            }

            var query = await _commonService
                ._unitOfWork
                .Repository<NotificationAppUserProfile, long>()
                .GetAllAsync(
                    r => r.AppUserProfileId == appUser.Id && r.IsRead == false,
                    null,
                    Including: "Notification");

            var notificationDtoList = _mapper.Map<List<NotificationAppUserProfileDto>>(query);

            if (notificationDtoList == null)
            {
                return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.NotificationIsNotFound, System.Net.HttpStatusCode.NotFound, Resource.NotificationNotFound);
            }

            return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.Success, System.Net.HttpStatusCode.OK, null, notificationDtoList);
        }

        public async Task<ApiResponse> CreateNewNotification(CreateNewNotificationDto createNewNotificationDtos)
        {
            Notification notification = new()
            {
                Entity = createNewNotificationDtos.Entity,
                Operation = createNewNotificationDtos.Operation,
                Status = createNewNotificationDtos.Status,
                AffectedRows = createNewNotificationDtos.AffectedRows,
                ParameterName = createNewNotificationDtos.ParameterName,
                From = createNewNotificationDtos.From,
                CreationDate = DateTimeHelper.Now,
                Type = createNewNotificationDtos.Type,
            };

            foreach (var item in createNewNotificationDtos.AppUserID)
            {
                notification.AppUserProfileNotifications.Add(new NotificationAppUserProfile()
                {
                    AppUserProfileId = item,
                    IsRead = false,
                });
            }

            await _commonService._unitOfWork.Repository<Notification, long>().AddAsync(notification);

            if (await _commonService._unitOfWork.Complete() > 0)

            {
                var notificationDto = new NotificationDto(
                    notification.Entity,
                    notification.Operation,
                    notification.From,
                    notification.Status,
                    notification.Type,
                    notification.AffectedRows
                );

                await _notificationHubService.NotifyAsync(
                    new(entity: notificationDto.Entity,
                    operation: notificationDto.Operation,
                    from: notificationDto.From,
                    status: notificationDto.Status,
                    type: notificationDto.Type,
                    affectedRows: notificationDto.AffectedRows,
                    parameterName: notificationDto.ParameterName),
                    [.. createNewNotificationDtos.AppUserID]
                );

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.NotificationAddedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.InternalServerError,
                Resource.FailedToCreateNotifications
            );
        }

        public async Task SendNotificationForNewItemBank(List<Guid> groupIds, CreateNewNotificationDto createNewNotification)
        {
            if (groupIds == null || !groupIds.Any())
                return;

            // Fetch the app users based on the group IDs
            var appusers = await _commonService
                ._unitOfWork
                .Repository<AppUserProfileGroup, long>()
                .GetAllAsync(x => groupIds.Contains(x.OESGroupId));

            var appUserIds = appusers.Select(z => z.AppUserProfileId).Distinct();

            createNewNotification.AppUserID = appUserIds.ToList();

            await CreateNewNotification(createNewNotification);
        }
    }
}
