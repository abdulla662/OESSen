using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.OesResources;
using OES.Helper.Dtos.User;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OES.Services.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IApiResponse _apiResponse;
        private readonly IConfiguration _configuration;
        private readonly ICommonService _commonService;
        private readonly IAPIMemoryCach _aPIMemoryCach;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        public readonly INotificationHubService _notificationHubService;

        public UserProfileService(IApiResponse apiResponse,
                                  IConfiguration configuration,
                                  ICommonService commonService,
                                  IAPIMemoryCach aPIMemoryCach,
                                  IMapper mapper,
                                  IHttpClientFactory httpClientFactory,
                                  INotificationHubService notificationHubService)
        {
            _apiResponse = apiResponse;
            _configuration = configuration;
            _commonService = commonService;
            _aPIMemoryCach = aPIMemoryCach;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
            _notificationHubService = notificationHubService;
        }

        public async Task<ApiResponse> AssignAndUnassignUserToGroupAsync(UsersToGroupDto usersToGroupDto)
        {
            if (usersToGroupDto == null)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ValidationNameRequired);
            }

            var repository = _commonService
                ._unitOfWork
                .Repository<AppUserProfileGroup, long>();

            var existingUserGroupRecords = (await repository
                .GetAllAsync(record => record.OESGroupId == usersToGroupDto.GroupId))
                .ToList();

            var affectedUserIds = existingUserGroupRecords
                .Select(x => x.AppUserProfileId)
                .Distinct()
                .ToHashSet();

            if (existingUserGroupRecords.Count > 0)
            {
                foreach (var record in existingUserGroupRecords)
                {
                    record.IsDeleted = true;
                }

                await _commonService._unitOfWork.Complete();
            }

            var newUserToGroupRelations = GetNewUserToGroupRelations(usersToGroupDto);

            if (newUserToGroupRelations != null && newUserToGroupRelations.Any())
            {
                repository.AddRangAsync(newUserToGroupRelations);

                await _commonService._unitOfWork.Complete();

                foreach (var userId in usersToGroupDto.UsersIds)
                {
                    affectedUserIds.Add(userId);
                }
            }

            if (affectedUserIds.Count > 0)
            {
                await _notificationHubService.NotifyAsync(
                    new NotificationDto
                    {
                        Type = NotificationTypeStatus.Warning,
                        ParameterName = "RolesUpdated"
                    },
                    affectedUserIds.ToArray()
                );
            }

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.UserToGroupRelationshipsHaveBeenUpdatedSuccessfully
            );
        }

        public async Task<ApiResponse> GetAllAsync()
        {
            var UsersInDB = await _commonService._unitOfWork.Repository<AppUserProfile, long>().GetAllAsync(null, x => x.OrderBy(x => x.Username));

            if (!UsersInDB.Any())
            {
                return _commonService
               ._apiResponse
               .GetApiResponse(CustomCodeStatus.NotFound,
                               HttpStatusCode.NotFound,
                               Resource.NotFound);
            }
            else
            {
                List<BlazUserDTO> UserDTOs = [];

                foreach (var user in UsersInDB)
                {
                    UserDTOs.Add(new BlazUserDTO { ID = user.Id, Name = user.Username });
                }

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null,
                                    UserDTOs);
            }
        }

        public async Task<ApiResponse> GetAllUserPaginationAsync(PaginationSearchModel pagination)
        {
            var query = _commonService._unitOfWork.Repository<AppUserProfile, long>().GetAll().AsNoTracking();

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey))
                {
                    query = query.Where(x => x.Username.Contains(pagination.SearchKey) || x.EmailAddress.Contains(pagination.SearchKey));

                }
                if (pagination.FromDate is not null)
                {
                    query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                             o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
                }

                query = pagination.OrderBy == SearchInKey.DESC ?
                        query.OrderByDescending(x => x.Username) :
                        query.OrderBy(x => x.Username);

                var totalItems = await query.CountAsync();

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize).ToListAsync();

                var mappedData = _mapper.Map<List<AppUserProfileRetrievalDto>>(data);

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  null,
                                                                  new CustomTableData<AppUserProfileRetrievalDto>(mappedData, totalItems));
            }
            else
            {
                var data = await (pagination.OrderBy == SearchInKey.DESC ?
                    query.OrderByDescending(x => x.Username).ToListAsync() :
                    query.OrderBy(x => x.Username).ToListAsync());

                var mappedData = _mapper.Map<List<AppUserProfileRetrievalDto>>(data);

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  null,
                                                                  new CustomTableData<AppUserProfileRetrievalDto>(mappedData, data.Count));
            }
        }

        public async Task<ApiResponse> GetUserGroupsAndRolesAsync(Guid userId)
        {
            // Get all user groups then all roles using userId:
            var userGroups = await _commonService
                            ._unitOfWork
                            .Repository<AppUserProfileGroup, long>()
                            .GetAllAsync(
                                u => u.AppUserProfileId == userId && !u.IsDeleted,
                                null,
                                "OESGroup.GroupResources.ResourceRoles.Role"
                            );

            var groupedUserGroups = _mapper.Map<List<UserGroupsAndRolesDto>>(userGroups);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                groupedUserGroups);
        }

        public async Task<ApiResponse> GetAssignedUserIdsByGroupAsync(Guid groupId)
        {
            var repo = _commonService._unitOfWork.Repository<AppUserProfileGroup, long>();

            var ids = await repo.GetAll()
                                .Where(x => x.OESGroupId == groupId && !x.IsDeleted)
                                .Select(x => x.AppUserProfileId)
                                .ToListAsync();

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, ids);
        }

        public async Task<ApiResponse> LogInAsync(LoginDto loginDto)
        {
            var httpClient = _httpClientFactory.CreateClient("SsoHttpClient");

            var httpResponse = await httpClient.PostAsJsonAsync("api/Account/LogIn", loginDto);

            var content = await httpResponse.Content.ReadAsStringAsync();

            //var decryptedContent = AesCipher.Decrypt(content); // This line is commented out because the response from the SSO service is not encrypted.

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return apiResponse!;
        }

        public async Task<ApiResponse> GetUserAccessByResourceTypeAsync(
            Guid userId,
            ResourceType resourceType)
        {
            if (resourceType == ResourceType.All || resourceType == ResourceType.Result)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new List<UserAccessByResourceDto>());
            }

            var includePath = GetIncludePath(resourceType);

            var userGroups = await _commonService
                ._unitOfWork
                .Repository<AppUserProfileGroup, long>()
                .GetAllAsync(
                    x => x.AppUserProfileId == userId && !x.IsDeleted,
                    Including: $"{nameof(AppUserProfileGroup.OESGroup)}.{includePath}"
                );

            var groupIds = userGroups
                .Where(ug => ug.OESGroup != null)
                .Select(ug => ug.OESGroup.Id)
                .Distinct()
                .ToList();

            var groupRoles = await _commonService
                ._unitOfWork
                .Repository<OESGroupRole, long>()
                .GetAll()
                .Where(gr => groupIds.Contains(gr.OESGroupId) && !gr.IsDeleted)
                .Include(gr => gr.OESRole)
                .ToListAsync();

            var rolesByGroupId = groupRoles
                .Where(gr => gr.OESRole != null)
                .GroupBy(gr => gr.OESGroupId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r => r.OESRole.Name)
                          .Distinct()
                          .ToList()
                );

            var result = new List<UserAccessByResourceDto>();

            foreach (var userGroup in userGroups)
            {
                var group = userGroup.OESGroup;
                if (group == null) continue;

                var resources = GetResourceDtos(group, resourceType);

                if (resources.Count == 0) continue;

                var roles = rolesByGroupId.TryGetValue(group.Id, out var groupRoleNames)
                    ? groupRoleNames
                    : [];

                result.Add(new UserAccessByResourceDto
                {
                    GroupId = group.Id,
                    GroupName = group.Name,
                    IsOwner = group.AutoCreatedForUser,
                    Resources = resources.ConvertAll(r => new ResourceAccessDto
                    {
                        ResourceId = r.Id,
                        ParentId = r.ParentId,
                        ParentName = r.ParentName,
                        ResourceName = r.Name,
                        Roles = roles
                    })
                });
            }

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                result);
        }

        private static List<(long Id, string Name, long? ParentId, string? ParentName)> GetResourceDtos(
            OESGroup group,
            ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.Questions => group.QuestionGroups?
                    .Where(x => x.Question != null)
                    .Select(x => (x.Question.Id, x.Question.Code ?? "", (long?)null, (string?)null))
                    .Distinct()
                    .ToList() ?? [],

                ResourceType.Papers => group.PaperGroups?
                    .Where(x => x.Paper != null)
                    .Select(x => (x.Paper.Id, x.Paper.Name ?? "", (long?)null, (string?)null))
                    .Distinct()
                    .ToList() ?? [],

                ResourceType.Schedule => group.ScheduleGroups?
                    .Where(x => x.Schedule != null)
                    .Select(x => (x.Schedule.Id, x.Schedule.Name ?? "", (long?)null, (string?)null))
                    .Distinct()
                    .ToList() ?? [],

                ResourceType.ItemBank => group.ItemBankGroups?
                    .Where(x => x.ItemBank != null)
                    .Select(x => (
                        x.ItemBank.Id,
                        x.ItemBank.Name ?? "",
                        x.ItemBank.ParentId,
                        x.ItemBank.ParentId != null
                            ? (group.ItemBankGroups.FirstOrDefault(p => p.ItemBank != null && p.ItemBank.Id == x.ItemBank.ParentId)?.ItemBank?.Name)
                            : null
                    ))
                    .Distinct()
                    .ToList() ?? [],

                ResourceType.Ilo => group.ILOGroups?
                    .Where(x => x.ILO != null)
                    .Select(x => (
                        x.ILO.Id,
                        x.ILO.Name ?? "",
                        x.ILO.ParentId,
                        x.ILO.ParentId != null
                            ? (group.ILOGroups.FirstOrDefault(p => p.ILO != null && p.ILO.Id == x.ILO.ParentId)?.ILO?.Name)
                            : null
                    ))
                    .Distinct()
                    .ToList() ?? [],

                ResourceType.Block => group.BlockGroups?
                    .Where(x => x.Block != null)
                    .Select(x => (x.Block.Id, x.Block.Name ?? "", (long?)null, (string?)null))
                    .Distinct()
                    .ToList() ?? [],

                _ => []
            };
        }

        #region Helper Methods
        public async Task<IApiResponse> ValidateTokenForUserAsync(string? token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]!));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = symmetricSecurityKey,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidateAudience = true,
                ValidAudiences = _configuration["JWT:ValidAudiences"]!.Split(", "),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var validationResult = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            var tokenValidationResultDto = new TokenValidationResultDto();

            if (validationResult.IsValid)
            {
                tokenValidationResultDto.IsValid = true;

                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null,
                                    tokenValidationResultDto);
            }

            tokenValidationResultDto.IsValid = false;

            return _apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.BadRequest,
                                null,
                                tokenValidationResultDto);
        }

        private static string GetIncludePath(ResourceType resourceType) => resourceType switch
        {
            ResourceType.Questions =>
                $"{nameof(OESGroup.QuestionGroups)}.{nameof(QuestionGroups.Question)}",

            ResourceType.Papers =>
                $"{nameof(OESGroup.PaperGroups)}.{nameof(PaperGroups.Paper)}",

            ResourceType.Schedule =>
                $"{nameof(OESGroup.ScheduleGroups)}.{nameof(ScheduleGroups.Schedule)}",

            ResourceType.ItemBank =>
                $"{nameof(OESGroup.ItemBankGroups)}.{nameof(ItemBankGroups.ItemBank)}",

            ResourceType.Ilo =>
                $"{nameof(OESGroup.ILOGroups)}.{nameof(ILOGroup.ILO)}",

            ResourceType.Block =>
                $"{nameof(OESGroup.BlockGroups)}.{nameof(BlockGroups.Block)}",

            _ => ""
        };

        private List<AppUserProfileGroup> GetNewUserToGroupRelations(UsersToGroupDto usersToGroupDto)
        {
            var newUserToGroupRelations = new List<AppUserProfileGroup>();

            foreach (var userId in usersToGroupDto.UsersIds)
            {
                newUserToGroupRelations.Add(new AppUserProfileGroup
                {
                    AppUserProfileId = userId,
                    OESGroupId = usersToGroupDto.GroupId,
                });
            }

            return newUserToGroupRelations;
        }
        #endregion
    }
}
