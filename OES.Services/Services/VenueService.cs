using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;
using System.Data;
using System.Net;
using System.Text.Json;
using Resource = OES.Helper.ResourceFiles.Resource;

namespace OES.Services.Services
{
    public class VenueService : IVenueService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;
        private readonly FilterParamsValues _filterParamsValues;

        public VenueService(ICommonService commonService, IMapper mapper, FilterParamsValues filterParamsValues)
        {
            _commonService = commonService;
            _mapper = mapper;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<IApiResponse> GetAllPaginatedVenuesAsync(PaginationSearchModel pagination)
        {
            var query = _commonService._unitOfWork.Repository<Venue, long>().GetAll().AsNoTracking();

            if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInName)
            {
                query = query.Where(x => x.Code.Contains(pagination.SearchKey));
            }

            if (!string.IsNullOrEmpty(pagination.SearchKey) && pagination.SearchInDescription)
            {
                query = query.Where(x => x.Name.Contains(pagination.SearchKey));
            }

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(o => o.CreationDate >= pagination.FromDate &&
                                         o.CreationDate < (pagination.ToDate ?? DateTime.Today).AddDays(1));
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalItems = await query.CountAsync();

            var paginatedData = await query.Skip(pagination.PageIndex * pagination.PageSize)
                                           .Take(pagination.PageSize)
                                           .ToListAsync();

            var subjectDtos = _mapper.Map<List<GetPaginatedVenueResponseDto>>(paginatedData);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.VenueFetchedSuccessfully,
                new CustomTableData<GetPaginatedVenueResponseDto>(subjectDtos, totalItems)
            );
        }

        public IApiResponse GetAllVenues()
        {
            var venueDtos = _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .Select(result => new GetVenueResponseDto(
                    result.Id,
                    result.Name,
                    result.Code,
                    result.TCIds,
                    result.Address,
                    result.GeoLocation ?? string.Empty,
                    result.PinCode,
                    result.VenueEmail ?? string.Empty,
                    result.Mobile ?? string.Empty,
                    result.CoordinatorFullName,
                    result.CoordinatorEmail ?? string.Empty,
                    result.CoordinatorMobile ?? string.Empty,
                    result.IPAddress ?? string.Empty,
                    result.Url ?? string.Empty
                ))
                .ToList();

            if (venueDtos.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.VenueNotFound);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.VenueFetchedSuccessfully,
                                    venueDtos);
        }

        public async Task<IApiResponse> GetVenueLookupAsync()
        {
            var venueDtos = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .Query()
                .Select(result => new VenueLookupDto(
                    result.Id,
                    result.DisplayName ?? result.Name
                ))
                .ToListAsync();

            if (!venueDtos.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.VenueNotFound);
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.VenueFetchedSuccessfully,
                venueDtos);
        }

        public async Task<IApiResponse> GetVenueById(long id)
        {
            var venue = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .GetByIdAsync(id);
            if (venue == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.VenueNotFound);
            }
            var venueDto = _mapper.Map<EditVenueRequestDto>(venue);
            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.VenueFetchedSuccessfully,
                                venueDto);
        }

        public async Task<IApiResponse> AddVenueAsync(AddVenueRequestDto venueDto, CancellationToken cancellationToken = default)
        {
            if (venueDto == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.VenueDataCannotBeNull);
            }

            var venueNameDuplication = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .IsExistAsync(v => v.Name.Trim().ToLower() == venueDto.Name.Trim().ToLower());

            var venueCodeDuplication = await _commonService
                ._unitOfWork
               .Repository<Venue, long>()
               .IsExistAsync(v => v.Code.Trim().ToLower() == venueDto.Code.Trim().ToLower());

            if (venueNameDuplication || venueCodeDuplication)
            {
                string message = string.Join(
                    " ",
                    venueNameDuplication ? Resource.VenueNameAlreadyExists : null,
                    venueCodeDuplication ? Resource.VenueCodeAlreadyExists : null
                );

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Conflict,
                    HttpStatusCode.Conflict,
                    message
                );
            }

            var venueEntity = _mapper.Map<Venue>(venueDto);

            await _commonService._unitOfWork.Repository<Venue, long>().AddAsync(venueEntity);

            await _commonService._unitOfWork.Complete();

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.VenueAddedSuccessfully);
        }

        public async Task<IApiResponse> AddMultipleVenuesAsync(AddMultipleVenuesRequestDto addMultipleVenuesRequestDto, CancellationToken cancellationToken = default)
        {
            using (var transaction = await _commonService._unitOfWork.BeginTransactionAsync())
            {
                try
                {
                    // Convert the DTO to JSON string for the stored procedure
                    string venueDataJson = JsonSerializer.Serialize(addMultipleVenuesRequestDto.AddVenueRequestDtos);
                    using (var command = _commonService._unitOfWork.CreateDbCommand())
                    {
                        command.Transaction = transaction.GetDbTransaction();
                        command.CommandText = "AddMultipleVenues";
                        command.CommandType = CommandType.StoredProcedure;

                        // Input parameters
                        var inputVenueData = new MySqlParameter("@VenueData", MySqlDbType.JSON)
                        {
                            Value = venueDataJson
                        };
                        var inputOrgSignature = new MySqlParameter("@OrgSignature", MySqlDbType.VarChar, 255)
                        {
                            Value = _filterParamsValues.Signature
                        };
                        var inputOrganizationId = new MySqlParameter("@OrganizationId", MySqlDbType.Int32)
                        {
                            Value = _filterParamsValues.OrganizationId
                        };

                        // Output parameters
                        var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                        {
                            Direction = ParameterDirection.Output
                        };
                        var errorMessageParam = new MySqlParameter("@errorMessage", MySqlDbType.Text)
                        {
                            Direction = ParameterDirection.Output
                        };

                        // Add parameters to command
                        command.Parameters.Add(inputVenueData);
                        command.Parameters.Add(inputOrgSignature);
                        command.Parameters.Add(inputOrganizationId);
                        command.Parameters.Add(returnStatusParam);
                        command.Parameters.Add(errorMessageParam);

                        // Execute the stored procedure
                        await _commonService._unitOfWork.OpenConnectionAsync();
                        await command.ExecuteNonQueryAsync();

                        // Get output values
                        int returnStatus = Convert.ToInt32(returnStatusParam.Value);
                        string errorMessage = Convert.ToString(errorMessageParam.Value) ?? string.Empty;

                        switch (returnStatus)
                        {
                            case 0:
                                await transaction.RollbackAsync();
                                return _commonService._apiResponse.GetApiResponse(
                                    CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    errorMessage);

                            case 1:
                                await transaction.CommitAsync();
                                return _commonService._apiResponse.GetApiResponse(
                                    CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    errorMessage);

                            case 2:
                                await transaction.CommitAsync();
                                return _commonService._apiResponse.GetApiResponse(
                                    CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.Accepted,
                                    errorMessage);

                            default:
                                await transaction.RollbackAsync();
                                return _commonService._apiResponse.GetApiResponse(
                                    CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.UnknownStatusReturnedFromStoredProcedure);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.InternalServerError,
                        $"Error: {ex.Message}");
                }
            }
        }

        public async Task<IApiResponse> EditVenueAsync(EditVenueRequestDto editVenueRequestDto)
        {
            if (editVenueRequestDto == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.VenueDataCannotBeNull);
            }

            var existingVenue = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .GetByIdAsync(editVenueRequestDto.Id);

            if (existingVenue == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.VenueNotFound);
            }

            var venueNameDuplication = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .IsExistAsync(v => v.Id != editVenueRequestDto.Id && v.Name.Trim().ToLower() == editVenueRequestDto.Name.Trim().ToLower());

            var venueCodeDuplication = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .IsExistAsync(v => v.Id != editVenueRequestDto.Id && v.Code.Trim().ToLower() == editVenueRequestDto.Code.Trim().ToLower());

            if (venueNameDuplication || venueCodeDuplication)
            {
                string message = string.Join(
                    " ",
                    venueNameDuplication ? Resource.VenueNameAlreadyExists : null,
                    venueCodeDuplication ? Resource.VenueCodeAlreadyExists : null
                );

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Conflict,
                    HttpStatusCode.Conflict,
                    message
                );
            }

            _mapper.Map(editVenueRequestDto, existingVenue);

            _commonService._unitOfWork.Repository<Venue, long>().Update(existingVenue);

            await _commonService._unitOfWork.Complete();

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.VenueUpdatedSuccessfully);
        }

        public async Task<IApiResponse> DeleteVenue(long id)
        {
            var venueRepo = _commonService._unitOfWork.Repository<Venue, long>();

            var venue = await venueRepo.GetObjAsync(x => x.Id == id);

            if (venue != null)
            {
                var checkingVenueDependenciesResult = await CheckVenueDependenciesAsync(venue.Id);

                if (!string.IsNullOrWhiteSpace(checkingVenueDependenciesResult))
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        checkingVenueDependenciesResult);
                }

                venueRepo.SoftDelete(venue);

                await _commonService._unitOfWork.Complete();

                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.VenueHasBeenDeletedSuccessfully);
            }
            else
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.VenueNotFound);
            }
        }

        //public async Task<IApiResponse> GetUserVenueGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";

        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Venue) &&
        //        (
        //            g.CreationUser.ToLower() == currentUser ||
        //            g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
        //        ),
        //        Including: nameof(OESGroup.GroupResources)
        //    );

        //    var groupDtos = groups
        //        .Select(g => new GetOESGroupDto
        //        {
        //            Id = g.Id,
        //            Name = g.Name
        //        })
        //        .DistinctBy(x => x.Id)
        //        .OrderBy(x => x.Name)
        //        .ToList();

        //    return _commonService._apiResponse.GetApiResponse(
        //        CustomCodeStatus.Success,
        //        HttpStatusCode.OK,
        //        Resource.SuccessfulFetching,
        //        groupDtos
        //    );
        //}

        //public async Task<IApiResponse> GetVenueGroupsAsync(long VenueId)
        //{
        //    var equationGroups = await _commonService
        //        ._unitOfWork
        //        .Repository<VenueGroups, long>()
        //        .GetAllAsync(
        //            x => x.VenueId == VenueId &&
        //                 !x.OESGroup.IsTemplate,
        //            Including: nameof(VenueGroups.OESGroup)
        //        );

        //    var venueGroupsDto = new VenueGroupsDto();

        //    equationGroups.ToList().ForEach(ibg => venueGroupsDto.GroupsIds.Add(ibg.OESGroupId));

        //    venueGroupsDto.OwnerGroupId = equationGroups
        //        .FirstOrDefault(x =>
        //            x.OESGroup != null &&
        //            x.OESGroup.AutoCreatedForUser
        //        )?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(
        //        CustomCodeStatus.Success,
        //        HttpStatusCode.OK,
        //        Resource.SuccessfulFetching,
        //        venueGroupsDto
        //    );
        //}


        #region Helper Methods
        private async Task<string> CheckVenueDependenciesAsync(long venueId)
        {
            var hasAssociatedSchedules = await _commonService
                ._unitOfWork
                .Repository<ScheduleVenue, long>()
                .Query()
                .AnyAsync(q => q.VenueId == venueId);

            if (hasAssociatedSchedules)
            {
                return Resource.VenueCannotBeDeletedDueToSchedules;
            }

            return null;
        }
        #endregion
    }
}