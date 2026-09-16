using ClosedXML.Excel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Schedule;
using OES.Core.Entities.Schedule.Views;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSummary;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class SchedulePaperService : ISchedulePaperService
    {
        private readonly ICommonService _commonService;
        private readonly ICBTCandidatesSyncService _cbtCandidatesSyncService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly IExamServerService _examServerService;

        public SchedulePaperService(ICommonService commonService, ICBTCandidatesSyncService cbtCandidatesSyncService, FilterParamsValues filterParamsValues, IExamServerService examServerService)
        {
            _commonService = commonService;
            _cbtCandidatesSyncService = cbtCandidatesSyncService;
            _filterParamsValues = filterParamsValues;
            _examServerService = examServerService;
        }

        public async Task<ApiResponse> GetAllSchedulePapersByScheduleIdAsync(PaginationSearchModel paginationSearchModel, long scheduleId)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetAll(x => x.ScheduleMetadataId == scheduleId, Including: nameof(SchedulePaper.PaperMetadata))
                .AsNoTracking();

            if (paginationSearchModel.PaginationOff)
            {
                var schedulePapersDtos = await MapToSchedulePaperDto(query).ToListAsync();

                if (schedulePapersDtos.Count == 0)
                {
                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoPaperFound);
                }

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, new CustomTableData<SchedulePaperPaginationDto>(schedulePapersDtos, schedulePapersDtos.Count));
            }

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey))
            {
                var searchKeyLower = paginationSearchModel.SearchKey.ToLower();

                if (paginationSearchModel.SearchInName && paginationSearchModel.SearchInDescription)
                {
                    query = query.Where(a => a.PaperMetadata.Name.ToLower().Contains(searchKeyLower) || a.PaperMetadata.Code.ToLower().Contains(searchKeyLower));
                }
                else if (paginationSearchModel.SearchInName)
                {
                    query = query.Where(a => a.PaperMetadata.Name.ToLower().Contains(searchKeyLower));
                }
                else if (paginationSearchModel.SearchInDescription)
                {
                    query = query.Where(a => a.PaperMetadata.Code.ToLower().Contains(searchKeyLower));
                }
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                var fromDate = DateOnly.FromDateTime(paginationSearchModel.FromDate.Value);
                query = query.Where(a => a.StartDate >= fromDate);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                var toDate = DateOnly.FromDateTime(paginationSearchModel.ToDate.Value);
                query = query.Where(a => a.EndDate <= toDate);
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoPaperFound);
            }

            var paginatedSchedulePapers = await MapToSchedulePaperDto(query)
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (paginatedSchedulePapers.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoPaperFound);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, new CustomTableData<SchedulePaperPaginationDto>(paginatedSchedulePapers, totalRecords));
        }

        public async Task<ApiResponse> GetSchedulePaperByIdAsync(long schedulePaperId)
        {
            var schedulePaper = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetAll(p => p.Id == schedulePaperId, null)
                .FirstOrDefaultAsync();

            if (schedulePaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduledPaperNotFound
                );
            }

            var schedulePaperFormsIds = _commonService
                ._unitOfWork
                .Repository<SchedulePaperForms, long>()
                .GetAll(p => p.SchedulePaperId == schedulePaperId, null)
                .Select(x => x.FormId)
                .ToList();

            var formsWithNotSyncedCandidatesIds = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .GetAll(c => c.SchedulePaperId == schedulePaperId && !c.IsSynced && c.PaperFormId.HasValue, null)
                .Select(c => c.PaperFormId.Value)
                .Distinct()
                .ToListAsync();

            var schedulePaperResponseDto = _commonService._mapper.Map<GetSchedulePaperResponseDto>(schedulePaper);

            schedulePaperResponseDto.SchedulePaperSelectedFormsIds = schedulePaperFormsIds;
            schedulePaperResponseDto.FormsWithNotSyncedCandidatesIds = formsWithNotSyncedCandidatesIds;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                schedulePaperResponseDto
            );
        }

        public async Task<ApiResponse> GetSchedulePaperSummaryAsync(long scheduleId)
        {
            if (scheduleId <= 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.BadRequest,
                        Resource.InvalidScheduleId
                    );
            }

            var scheduleMetadata = await _commonService
                ._unitOfWork
                .Repository<ScheduleMetadata, long>()
                .GetByIdAsync(scheduleId);

            if (scheduleMetadata == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.NotFound,
                        HttpStatusCode.NotFound,
                        Resource.ScheduleNotFound
                    );
            }

            var schedulePaperRecords = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperView, long>()
                .GetAllAsync(x => x.ScheduleMetadataId == scheduleId);

            var scheduleSummary = MapToScheduleSummaryDto(scheduleMetadata, schedulePaperRecords);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ScheduleSummaryRetrievedSuccessfully,
                    scheduleSummary
                );
        }

        public async Task<ApiResponse> AddSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto addSchedulePaperRequestDto)
        {
            var validationResult = await ValidateAddingOrUpdatingSchedulePaperAsync(addSchedulePaperRequestDto);

            if (!validationResult.IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    validationResult.Message
                );
            }

            var schedulePaper = _commonService._mapper.Map<SchedulePaper>(addSchedulePaperRequestDto);

            foreach (var formId in addSchedulePaperRequestDto.SchedulePaperSelectedFormsIds)
            {
                schedulePaper.SchedulePaperForms.Add(
                    new SchedulePaperForms
                    {
                        FormId = formId
                    });
            }

            await _commonService._unitOfWork.Repository<SchedulePaper, long>().AddAsync(schedulePaper);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaperScheduledSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.BadRequest,
                Resource.ErrorCannotSchedulePaper
            );
        }

        public async Task<ApiResponse> UpdateSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto updateSchedulePaperRequestDto)
        {
            var validationResult = await ValidateAddingOrUpdatingSchedulePaperAsync(updateSchedulePaperRequestDto);

            if (!validationResult.IsValid)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    validationResult.Message
                );
            }

            var existingSchedulePaper = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetObjAsync(x => x.Id == updateSchedulePaperRequestDto.SchedulePaperId);

            if (existingSchedulePaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.SchedulePaperNotFound
                );
            }

            _commonService._mapper.Map(updateSchedulePaperRequestDto, existingSchedulePaper);

            var schedulePaperFormRepo = _commonService._unitOfWork.Repository<SchedulePaperForms, long>();

            var existingForms = await schedulePaperFormRepo
                .Query()
                .IgnoreQueryFilters()
                .Where(f => f.SchedulePaperId == existingSchedulePaper.Id)
                .ToListAsync();

            var existingFormIds = new HashSet<long>(existingForms.Select(f => f.FormId));

            var newFormIds = new HashSet<long>(updateSchedulePaperRequestDto.SchedulePaperSelectedFormsIds);

            var toRemove = existingForms.Where(f => !newFormIds.Contains(f.FormId)).ToList();

            if (toRemove.Count > 0)
            {
                schedulePaperFormRepo.SoftDeleteRange(toRemove);
            }

            var toAdd = newFormIds.Where(id => !existingFormIds.Contains(id)).ToList();

            var toReturn = existingForms.Where(id => newFormIds.Contains(id.FormId) && id.IsDeleted).ToList();

            if (toAdd.Count > 0)
            {
                foreach (var formId in toAdd)
                {
                    existingSchedulePaper.SchedulePaperForms.Add(new SchedulePaperForms
                    {
                        FormId = formId
                    });
                }
            }

            if (toReturn.Count > 0)
            {
                foreach (var form in toReturn)
                {
                    form.IsDeleted = false;
                    form.IsActive = true;
                }
            }

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SchedulePaperUpdatedSuccessfully
                );
        }

        public async Task<ApiResponse> DeleteSchedulePaperAsync(long schedulePaperId)
        {
            var countOfCandidatesAssignedOnSchedulePaper = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .GetAll(x => x.SchedulePaperId == schedulePaperId)
                .LongCountAsync();

            if (countOfCandidatesAssignedOnSchedulePaper > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CannotDeleteSchedulePaperWithCandidates
                );
            }

            var existingSchedulePaper = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetObjAsync(x => x.Id == schedulePaperId, Including: nameof(SchedulePaper.PaperSettings));

            if (existingSchedulePaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.SchedulePaperNotFound
                );
            }

            if (existingSchedulePaper.PaperSettings != null)
            {
                _commonService._unitOfWork.Repository<SchedulePaperSettings, long>().SoftDelete(existingSchedulePaper.PaperSettings);
            }

            _commonService._unitOfWork.Repository<SchedulePaper, long>().SoftDelete(existingSchedulePaper);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SchedulePaperAndSettingsDeletedSuccessfully
            );
        }

        public async Task<ApiResponse> GetSchedulePaperAllocation(long schedulePaperId)
        {
            var schedulePaperAllocation = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperAllocationView, long>()
                .GetAllAsync(x => x.SchedulePaperId == schedulePaperId);

            if (schedulePaperAllocation == null || !schedulePaperAllocation.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCandidatesAllocatedToThisSchedulePaper
                );
            }

            var schedulePaperAllocationDto = _commonService._mapper.Map<List<GetSchedulePaperAllocationResponseDto>>(schedulePaperAllocation);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                schedulePaperAllocationDto
            );
        }

        public async Task<ApiResponse> DeletePaperAllocationAsync(long venueId, long schedulePaperId)
        {
            var schedulePaperAllocation = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .GetAllAsync(x => x.VenueId == venueId && x.SchedulePaperId == schedulePaperId);

            if (schedulePaperAllocation == null || !schedulePaperAllocation.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoCandidatesAllocatedToThisSchedulePaperInVenue
                );
            }

            _commonService._unitOfWork.Repository<SchedulePaperCandidate, long>().DeleteRange(schedulePaperAllocation);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesAllocationDeletedSuccessfully
            );
        }

        public async Task<ApiResponse> GetSchedulePapersAsync(long scheduleMetadataId)
        {
            var schedulePapersDtos = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetAll(x => x.ScheduleMetadataId == scheduleMetadataId)
                .AsNoTracking()
                .Select(sp => new GetSchedulePaperTimeConfiguration
                {
                    Id = sp.Id,
                    PaperId = sp.PaperId,
                    StartDate = sp.StartDate,
                    EndDate = sp.EndDate,
                    StartTime = sp.StartTime,
                    EndTime = sp.EndTime
                })
                .ToListAsync();

            if (schedulePapersDtos.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(
                        CustomCodeStatus.NotFound,
                        HttpStatusCode.NotFound,
                        Resource.NoPaperFound
                    );
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    schedulePapersDtos
                );
        }

        public async Task<ApiResponse> GetVenuesWithCandidatesCountByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            var baseQuery = _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .GetAll(x => x.SchedulePaperId == paperId)
                .AsNoTracking()
                .Include(x => x.Venue)
                .AsQueryable();

            if (paginationSearchModel.FromDate.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            var query = baseQuery
                .GroupBy(x => new { x.VenueId, x.Venue.Name, x.Venue.Code })
                .Select(g => new VenueCandidatesCountDto
                {
                    VenueId = g.Key.VenueId,
                    VenueName = g.Key.Name,
                    VenueCode = g.Key.Code,
                    CandidatesCount = g.Count()
                });

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                query = query.Where(x => x.VenueName.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()) || x.VenueCode.ToLower().Contains(paginationSearchModel.SearchKey.ToLower()));
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ?
                query.OrderByDescending(x => x.VenueName) :
                query.OrderBy(x => x.VenueName);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoVenuesFoundForThisPaper);
            }

            var paginatedData = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                new CustomTableData<VenueCandidatesCountDto>(paginatedData, totalRecords));
        }

        public async Task<ApiResponse> GetSchedulePaperVenuesAsync(long schedulePaperId)
        {
            var schedulePaper = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetObjAsync(p => p.Id == schedulePaperId);

            if (schedulePaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduledPaperNotFound
                );
            }

            var venues = await _commonService
                ._unitOfWork
                .Repository<ScheduleVenue, long>()
                .GetAll(v => v.ScheduleId == schedulePaper.ScheduleMetadataId)
                .AsNoTracking()
                .Select(v => new VenueResponseDto
                {
                    VenueCode = v.Venue.Code,
                    VenueName = v.Venue.Name
                })
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.VenuesRetrievedSuccessfully,
                venues
            );
        }

        public async Task<byte[]> ExportSchedulePaperCandidatesToExcelAsync(long schedulePaperId)
        {
            var candidates = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .GetAll(x => x.SchedulePaperId == schedulePaperId)
                .AsNoTracking()
                .Select(c => new
                {
                    c.RegistrationNumber,
                    c.Candidate.CandidateCode,
                    c.Candidate.Name,
                    c.Candidate.NationalId,
                    c.Candidate.Email,
                    c.Candidate.Mobile,
                    c.Candidate.Gender,
                    c.Candidate.DateOfBirth,
                    c.Candidate.Qualification,
                    c.Candidate.Address,
                    c.Candidate.RegistrationCenterCode,
                    VenueName = c.Venue.Name,
                    VenueCode = c.Venue.Code,
                    c.CandidateExamDate
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Candidates");

            var headers = new[]
            {
                Resource.RegistrationNumber,
                Resource.CandidateCode,
                Resource.CandidateName,
                Resource.NationalId,
                Resource.Email,
                Resource.Mobile,
                Resource.Gender,
                Resource.DateOfBirth,
                Resource.Qualification,
                Resource.Address,
                Resource.RegistrationCenterCode,
                Resource.VenueName,
                Resource.VenueCode,
                Resource.CandidateExamDate
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            for (int row = 0; row < candidates.Count; row++)
            {
                var candidate = candidates[row];
                worksheet.Cell(row + 2, 1).Value = candidate.RegistrationNumber.ToString();
                worksheet.Cell(row + 2, 2).Value = candidate.CandidateCode ?? string.Empty;
                worksheet.Cell(row + 2, 3).Value = candidate.Name ?? string.Empty;
                worksheet.Cell(row + 2, 4).Value = candidate.NationalId ?? string.Empty;
                worksheet.Cell(row + 2, 5).Value = candidate.Email ?? string.Empty;
                worksheet.Cell(row + 2, 6).Value = candidate.Mobile ?? string.Empty;
                worksheet.Cell(row + 2, 7).Value = candidate.Gender.ToString();
                worksheet.Cell(row + 2, 8).Value = candidate.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty;
                worksheet.Cell(row + 2, 9).Value = candidate.Qualification ?? string.Empty;
                worksheet.Cell(row + 2, 10).Value = candidate.Address ?? string.Empty;
                worksheet.Cell(row + 2, 11).Value = candidate.RegistrationCenterCode ?? string.Empty;
                worksheet.Cell(row + 2, 12).Value = candidate.VenueName ?? string.Empty;
                worksheet.Cell(row + 2, 13).Value = candidate.VenueCode ?? string.Empty;
                worksheet.Cell(row + 2, 14).Value = candidate.CandidateExamDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            }

            worksheet.Columns().AdjustToContents();

            await using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<ApiResponse> GetVenueBatchesAsync(long venueId, long paperId)
        {
            var batches = await _commonService
                ._unitOfWork.Repository<SchedulePaperCandidate, long>()
                .GetAll(x => x.SchedulePaperId == paperId && x.VenueId == venueId)
                .AsNoTracking()
                .GroupBy(x => x.BatchId)
                .Select(g => new VenueBatchDetailsDto(
                    g.Key,
                    g.Max(x => x.CreationDate),
                    g.Count()
                )).ToListAsync();

            if (batches.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    batches
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.NotFound,
                HttpStatusCode.NotFound,
                Resource.NoBatchesFound
            );
        }

        public async Task<ApiResponse> GetSchedulePaperCandidateByRegistrationNumberAsync(long registrationNumber)
        {
            var record = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .GetAll(x => x.RegistrationNumber == registrationNumber)
                .AsNoTracking()
                .Include(x => x.Candidate)
                .Include(x => x.Venue)
                .Include(x => x.SchedulePaper)
                    .ThenInclude(sp => sp.PaperMetadata)
                .Include(x => x.SchedulePaper)
                    .ThenInclude(sp => sp.ScheduleMetadata)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CandidateNotFound
                );
            }

            var hasAnswers = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .GetAll(x => x.RegistrationId == registrationNumber)
                .AnyAsync();

            var dto = new SchedulePaperCandidateDetailsDto
            {
                Id = record.Id,
                CandidateId = record.CandidateId,
                CandidateName = record.Candidate.Name,
                NationalId = record.Candidate.NationalId,
                RegistrationNumber = record.RegistrationNumber,
                PaperName = record.SchedulePaper.PaperMetadata.Name,
                ScheduleName = record.SchedulePaper.ScheduleMetadata.Name,
                Status = record.IsSynced ? "Synced" : "Scheduled",
                CandidateExamDate = record.CandidateExamDate,
                VenueName = record.Venue.Name,
                VenueCode = record.Venue.Code,
                CenterCode = record.Candidate.RegistrationCenterCode,
                VenueId = record.VenueId,
                SchedulePaperId = record.SchedulePaperId,
                HasAnswers = hasAnswers
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                dto
            );
        }

        public async Task<ApiResponse> UpdateSchedulePaperCandidateAsync(UpdateSchedulePaperCandidateRequestDto request)
        {
            var newVenue = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .GetObjAsync(x => x.Id == request.NewVenueId);

            if (newVenue == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.VenueNotFound);

            var newCenterCode = request.NewCenterCode?.Trim();

            if (string.IsNullOrWhiteSpace(newCenterCode))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.CodeIsRequired);
            }

            var tcIds = (newVenue.TCIds ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (!tcIds.Contains(newCenterCode))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.CenterCodeNotFoundForTheSelectedVenue);
            }

            var spc = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .GetObjAsync(x => x.Id == request.SchedulePaperCandidateId);

            if (spc == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CandidateNotFound);
            }

            var hasAnswers = await _commonService
                ._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .GetAll(x => x.RegistrationId == spc.RegistrationNumber)
                .AnyAsync();

            if (hasAnswers)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.ThisCandidateHasAlreadyBeenExaminedNoChangesAllowed);
            }

            var candidate = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .GetObjAsync(x => x.Id == spc.CandidateId);

            if (candidate == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CandidateNotFound);
            }

            if (spc.CandidateExamDate == request.NewExamDate
                && spc.VenueId == request.NewVenueId
                && string.Equals(candidate.RegistrationCenterCode?.Trim(), newCenterCode, StringComparison.OrdinalIgnoreCase))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.NothingToUpdateTheExamDateVenueAndCenterCodeAreTheCandidatesCurrentDetails);
            }

            spc.VenueId = request.NewVenueId;
            spc.CandidateExamDate = request.NewExamDate;
            spc.IsSynced = false;

            candidate.RegistrationCenterCode = newCenterCode;
            candidate.IsSynced = false;

            var modifiedByEmail = _filterParamsValues.UserEmail ?? "OesService";

            await _commonService._unitOfWork.Complete();

            var warnings = new List<string>();

            var notifyResponse = await _cbtCandidatesSyncService.NotifyOldVenueCesAsync(request.RegistrationNumber, request.OldVenueCode, modifiedByEmail);

            if (notifyResponse.StatusCode != HttpStatusCode.OK)
                warnings.Add(notifyResponse.Message);

            var resyncResponse = await ResyncCandidateToNewVenueAsync(spc.SchedulePaperId, request.NewVenueId, spc.CandidateId);

            if (resyncResponse.StatusCode is not HttpStatusCode.Accepted)
                warnings.Add(string.Format(Resource.TheCandidateWasNotSentToTheNewVenueYet, resyncResponse.Message));

            var message = warnings.Count == 0
                ? Resource.CandidateUpdatedSuccessfully
                : $"{Resource.CandidateUpdatedSuccessfully} {string.Join(" ", warnings)}";

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                message);
        }


        #region Helper Methods

        private async Task<ApiResponse> ResyncCandidateToNewVenueAsync(long schedulePaperId, long newVenueId, long candidateId)
        {
            var scheduleId = await _commonService._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetAll(x => x.Id == schedulePaperId)
                .AsNoTracking()
                .Select(x => x.ScheduleMetadataId)
                .FirstOrDefaultAsync();

            if (scheduleId == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScheduleNotFound);
            }

            return await _examServerService.BulkSync(
                scheduleId,
                isAutoSync: true,
                venueIds: [newVenueId],
                candidateIdsParam: [candidateId]);
        }

        private async Task<(bool IsValid, string Message)> ValidateAddingOrUpdatingSchedulePaperAsync(AddOrUpdateSchedulePaperRequestDto addOrUpdateSchedulePaperRequestDto)
        {
            var startDateTime = addOrUpdateSchedulePaperRequestDto.StartDate.ToDateTime(addOrUpdateSchedulePaperRequestDto.StartTime);
            var endDateTime = addOrUpdateSchedulePaperRequestDto.EndDate.ToDateTime(addOrUpdateSchedulePaperRequestDto.EndTime);

            if (endDateTime <= startDateTime)
            {
                return (false, Resource.EndDateTimeMustBeAfterStartDateTime);
            }

            var hasConflict = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetAll()
                .AsNoTracking()
                .AnyAsync(sp =>
                    sp.PaperId == addOrUpdateSchedulePaperRequestDto.PaperId &&
                    sp.Id != addOrUpdateSchedulePaperRequestDto.SchedulePaperId &&
                    sp.StartDate.ToDateTime(sp.StartTime) <= endDateTime &&
                    sp.EndDate.ToDateTime(sp.EndTime) >= startDateTime
                );

            if (hasConflict)
            {
                return (false, Resource.SchedulePaperConflict);
            }

            return (true, string.Empty);
        }

        private static GetScheduleSummaryDto MapToScheduleSummaryDto(ScheduleMetadata scheduleMetadata, IEnumerable<SchedulePaperView> paperRecords)
        {
            var summary = new GetScheduleSummaryDto
            {
                ScheduleMetadataId = scheduleMetadata.Id,
                ScheduleName = scheduleMetadata.Name,
                ScheduleCode = scheduleMetadata.Code,
                ScheduleDescription = scheduleMetadata.Description,
                ScheduleStartDate = scheduleMetadata.StartDate,
                ScheduleEndDate = scheduleMetadata.EndDate,
                ScheduleStartTime = scheduleMetadata.StartTime,
                ScheduleEndTime = scheduleMetadata.EndTime,
                ScheduleLocation = scheduleMetadata.ScheduleLocation,
                SchedulePublishingStatus = scheduleMetadata.PublishingStatus,
                Papers = [.. paperRecords.Select(p => new GetSchedulePaperSummaryDto
                {
                    PaperId = p.PaperId,
                    PaperName = p.PaperName,
                    PaperCode = p.PaperCode,
                    PaperType = p.PaperType,
                    AdaptiveSubtype = p.AdaptiveSubtype,
                    PaperQuestionSelectionType = p.PaperQuestionSelectionType,
                    StartDate = p.PaperStartDate,
                    EndDate = p.PaperEndDate,
                    StartTime = p.PaperStartTime,
                    EndTime = p.PaperEndTime,
                    SchedulePaperDescription = p.SchedulePaperDescription
                })]
            };

            return summary;
        }

        private static IQueryable<SchedulePaperPaginationDto> MapToSchedulePaperDto(IQueryable<SchedulePaper> query)
        {
            return query.Select(sp => new SchedulePaperPaginationDto
            {
                Id = sp.Id,
                PaperId = sp.PaperId,
                PaperName = sp.PaperMetadata.Name,
                PaperCode = sp.PaperMetadata.Code,
                SessionDescription = sp.Description,
                StartDate = sp.StartDate,
                EndDate = sp.EndDate,
                StartTime = sp.StartTime,
                EndTime = sp.EndTime,
                PaperSettingsConfigured = sp.PaperSettings != null,
                CandidatesCount = sp.Candidates.LongCount()
            });
        }

        #endregion
    }
}