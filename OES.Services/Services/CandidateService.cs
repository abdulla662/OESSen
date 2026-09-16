using ClosedXML.Excel;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using OES.Core.Entities;
using OES.Core.Entities.CBTCandidates;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.CandidateBatchImportHistory.Requests;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.OrganizationStructure.Responses;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.DocLibBackEndHttpClientHelper;
using OES.Helper.General.NewApiResponse;
using OES.Interface.GenericMemoryCacheRepository;
using OES.Interface.Interfaces;
using OES.Services.Helpers;
using SharedHelper.General;
using System.Data;
using System.Net;
using System.Text.Json;
using Resource = OES.Helper.ResourceFiles.Resource;

namespace OES.Services.Services
{
    public class CandidateService : ICandidateService
    {
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly DocLibBackEndHttpClientHelper _docLibBackEndHttpClient;
        private readonly IFileProcessingService<AddMultipleCandidateDto> _candidateFileProcessingService;
        private readonly IFileProcessingService<DumpImportCandidateRequestDto> _importToScheduleFileProcessingService;
        private readonly IMemoryCacheRepository _memoryCacheRepository;
        private readonly INotificationHubService _notificationHubService;
        private readonly ICandidateBatchImportHistoryService _candidateBatchImportHistoryService;
        private readonly IWebHostEnvironment _env;

        public CandidateService(
            ICommonService commonService,
            DocLibBackEndHttpClientHelper docLibBackEndHttpClient,
            FilterParamsValues filterParamsValues,
            IFileProcessingService<AddMultipleCandidateDto> candidateFileProcessingService,
            IFileProcessingService<DumpImportCandidateRequestDto> importToScheduleFileProcessingService,
            IMemoryCacheRepository memoryCacheRepository,
            INotificationHubService notificationHubService,
            ICandidateBatchImportHistoryService candidateBatchImportHistoryService,
            IWebHostEnvironment env
        )
        {
            _candidateFileProcessingService = candidateFileProcessingService;
            _importToScheduleFileProcessingService = importToScheduleFileProcessingService;
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
            _docLibBackEndHttpClient = docLibBackEndHttpClient;
            _memoryCacheRepository = memoryCacheRepository;
            _notificationHubService = notificationHubService;
            _candidateBatchImportHistoryService = candidateBatchImportHistoryService;
            _env = env;
        }

        public async Task<ApiResponse> GetAllCandidatesAsync(PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .Query()
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey))
            {
                var searchKey = paginationSearchModel.SearchKey.Trim().ToLower();

                query = query.Where(x =>
                    (paginationSearchModel.SearchInName && x.UserName.ToLower().Contains(searchKey)) ||
                    (paginationSearchModel.SearchInBody && x.NationalId.Contains(searchKey)) ||
                    (paginationSearchModel.SearchInDescription && x.Email.ToLower().Contains(searchKey))
                );
            }

            if (paginationSearchModel.FromDate.HasValue)
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);

            if (paginationSearchModel.ToDate.HasValue)
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoCandidatesFound);
            }

            query = paginationSearchModel.OrderBy?.ToUpper() == "DESC"
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

            if (!paginationSearchModel.PaginationOff)
            {
                int pageSize = paginationSearchModel.PageSize > 0 ? paginationSearchModel.PageSize : 10;
                query = query.Skip(paginationSearchModel.PageIndex * pageSize).Take(pageSize);
            }

            var candidates = await query.ToListAsync();

            var candidateDtos = candidates.ConvertAll(result => new CandidatesListResponseDto(
                result.Id,
                result.Name,
                result.Email,
                result.UserName,
                result.Mobile,
                result.CandidateCode,
                result.NationalId));

            var tableData = new CustomTableData<CandidatesListResponseDto>(candidateDtos, totalRecords);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesFetchedSuccessfully,
                tableData);
        }

        public async Task<ApiResponse> GetCandidatesWithExamDateAroundAsync(PaginationSearchModel paginationSearchModel)
        {
            var today = DateTime.Today;
            var dayAfter = today.AddDays(2);
            var tomorrow = today.AddDays(1);

            var schedulePaperCandidateRepo = _commonService._unitOfWork.Repository<SchedulePaperCandidate, long>();

            var rangeStart = today;
            var rangeExclusiveEnd = dayAfter.AddDays(1);

            var candidateIds = await schedulePaperCandidateRepo.Query()
                .AsNoTracking()
                .Where(spc => spc.CandidateExamDate.HasValue &&
                              spc.CandidateExamDate.Value >= rangeStart &&
                              spc.CandidateExamDate.Value < rangeExclusiveEnd)
                .Select(spc => spc.CandidateId)
                .Distinct()
                .ToListAsync();

            if (candidateIds == null || candidateIds.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoCandidatesFound);
            }

            var query = _commonService._unitOfWork.Repository<Candidate, long>()
                .Query()
                .AsNoTracking()
                .Include(c => c.Disability)
                .Where(c => candidateIds.Contains(c.Id));

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey))
            {
                var searchKey = paginationSearchModel.SearchKey.Trim().ToLower();

                query = query.Where(x =>
                    (paginationSearchModel.SearchInName && x.CandidateCode.ToLower().Contains(searchKey)) ||
                    (paginationSearchModel.SearchInBody && x.NationalId.Contains(searchKey)) ||
                    (paginationSearchModel.SearchInDescription && x.Email.ToLower().Contains(searchKey))
                );
            }

            if (paginationSearchModel.FromDate.HasValue)
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);

            if (paginationSearchModel.ToDate.HasValue)
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoCandidatesFound);
            }

            query = paginationSearchModel.OrderBy?.ToUpper() == "DESC"
                ? query.OrderByDescending(x => x.Id)
                : query.OrderBy(x => x.Id);

            if (!paginationSearchModel.PaginationOff)
            {
                int pageSize = paginationSearchModel.PageSize > 0 ? paginationSearchModel.PageSize : 10;
                query = query.Skip(paginationSearchModel.PageIndex * pageSize).Take(pageSize);
            }

            var candidates = await query.ToListAsync();

            var candidateDtos = candidates.ConvertAll(result => new CandidatesListResponseDto(
                result.Id,
                result.Name,
                result.Email,
                result.UserName,
                result.Mobile,
                result.CandidateCode,
                result.NationalId,
                result.Disability?.Name));

            var tableData = new CustomTableData<CandidatesListResponseDto>(candidateDtos, totalRecords);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesFetchedSuccessfully,
                tableData);
        }

        public async Task<ApiResponse> GetCandidateByIdAsync(long candidateId)
        {
            var candidate = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .GetObjAsync(result => result.Id == candidateId, Including: nameof(Candidate.CandidateOrganizationNodeLookupItems));

            if (candidate is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.CandidateNotFound);
            }

            var candidateDto = _commonService._mapper.Map<AddOrUpdateCandidateRequestDto>(candidate);

            return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.Success,
                                   HttpStatusCode.OK,
                                   Resource.CandidateFetchedSuccessfully,
                                   candidateDto);
        }

        public async Task<ApiResponse> AddCandidateAsync(CandidateDto addCandidateRequestDto)
        {
            var duplicateCandidate = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .GetObjAsync(c =>
                    c.NationalId.Trim().ToLower() == addCandidateRequestDto.NationalId.Trim().ToLower() ||
                    c.CandidateCode.Trim().ToLower() == addCandidateRequestDto.CandidateCode.Trim().ToLower() ||
                    c.Email.Trim().ToLower() == addCandidateRequestDto.Email.Trim().ToLower()
                );

            if (duplicateCandidate != null)
            {
                var errors = new List<string>();

                if (duplicateCandidate.NationalId.Equals(addCandidateRequestDto.NationalId, StringComparison.OrdinalIgnoreCase))
                    errors.Add(Resource.CandidateNationalIdAlreadyExists);

                if (duplicateCandidate.CandidateCode.Equals(addCandidateRequestDto.CandidateCode, StringComparison.OrdinalIgnoreCase))
                    errors.Add(Resource.CandidateCodeAlreadyExists);

                if (duplicateCandidate.Email.Equals(addCandidateRequestDto.Email, StringComparison.OrdinalIgnoreCase))
                    errors.Add(Resource.CandidateEmailAlreadyExists);

                string message = string.Join($" {Resource.And} ", errors);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Conflict,
                    HttpStatusCode.Conflict,
                    message
                );
            }

            var candidate = _commonService._mapper.Map<Candidate>(addCandidateRequestDto);
            candidate.HasDisability = addCandidateRequestDto.HasDisability;
            candidate.DisabilityId = addCandidateRequestDto?.DisabilityId;

            if (addCandidateRequestDto.PhotoFile != null)
            {
                var photoUrl = await UploadFileToDocLib(addCandidateRequestDto.PhotoFile);

                if (string.IsNullOrWhiteSpace(photoUrl))
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.BadRequest,
                                        Resource.FailedToUploadPersonalPhoto);
                }

                candidate.PhotoURL = photoUrl;
            }

            if (addCandidateRequestDto.SignatureFile != null)
            {
                var signatureUrl = await UploadFileToDocLib(addCandidateRequestDto.SignatureFile);

                if (string.IsNullOrWhiteSpace(signatureUrl))
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.BadRequest,
                                        Resource.FailedToUploadSignaturePhoto);
                }

                candidate.SignatureURL = signatureUrl;
            }

            await _commonService._unitOfWork.Repository<Candidate, long>().AddAsync(candidate);

            var rowsAffected = await _commonService._unitOfWork.Complete();

            if (rowsAffected == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailedToAddCandidate);
            }

            return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.Success,
                                   HttpStatusCode.OK,
                                   Resource.CandidateHasBeenAddedSuccessfully);
        }

        public async Task<ApiResponse> UpdateCandidateAsync(CandidateDto updateCandidateRequestDto)
        {
            var duplicateCandidate = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .GetObjAsync(c => c.Id != updateCandidateRequestDto.CandidateId &&
                (
                    c.NationalId.Trim().ToLower() == updateCandidateRequestDto.NationalId.Trim().ToLower() ||
                    c.CandidateCode.Trim().ToLower() == updateCandidateRequestDto.CandidateCode.Trim().ToLower() ||
                    c.Email.Trim().ToLower() == updateCandidateRequestDto.Email.Trim().ToLower()
                ));

            if (duplicateCandidate != null)
            {
                var errors = new List<string>();

                if (duplicateCandidate.NationalId.Equals(updateCandidateRequestDto.NationalId, StringComparison.OrdinalIgnoreCase))
                    errors.Add(Resource.CandidateNationalIdAlreadyExists);

                if (duplicateCandidate.CandidateCode.Equals(updateCandidateRequestDto.CandidateCode, StringComparison.OrdinalIgnoreCase))
                    errors.Add(Resource.CandidateCodeAlreadyExists);

                if (duplicateCandidate.Email.Equals(updateCandidateRequestDto.Email, StringComparison.OrdinalIgnoreCase))
                    errors.Add(Resource.CandidateEmailAlreadyExists);

                string message = string.Join($" {Resource.And} ", errors);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Conflict,
                    HttpStatusCode.Conflict,
                    message
                );
            }

            var candidate = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .GetObjAsync(result => result.Id == updateCandidateRequestDto.CandidateId, Including: nameof(Candidate.CandidateOrganizationNodeLookupItems));

            if (candidate is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.CandidateNotFound);
            }

            var originalPhotoUrl = candidate.PhotoURL;

            var originalSignatureUrl = candidate.SignatureURL;

            _commonService._mapper.Map(updateCandidateRequestDto, candidate);

            if (updateCandidateRequestDto.PhotoFile != null)
            {
                if (!string.IsNullOrWhiteSpace(candidate.PhotoURL) && Guid.TryParse(candidate.PhotoURL, out var oldPhotoId))
                {
                    await DeleteFileFromDocLib(oldPhotoId);
                }

                var photoUrl = await UploadFileToDocLib(updateCandidateRequestDto.PhotoFile);

                if (string.IsNullOrWhiteSpace(photoUrl))
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.BadRequest,
                                        Resource.FailedToUploadProfilePictureTryAgain);
                }

                candidate.PhotoURL = photoUrl;
            }
            else
            {
                candidate.PhotoURL = originalPhotoUrl;
            }

            if (updateCandidateRequestDto.SignatureFile != null)
            {
                if (!string.IsNullOrWhiteSpace(candidate.SignatureURL) && Guid.TryParse(candidate.SignatureURL, out var oldSignatureId))
                {
                    await DeleteFileFromDocLib(oldSignatureId);
                }

                var signatureUrl = await UploadFileToDocLib(updateCandidateRequestDto.SignatureFile);

                if (string.IsNullOrWhiteSpace(signatureUrl))
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Failure,
                                        HttpStatusCode.BadRequest,
                                        Resource.FailedToUploadSignatureImageTryAgain);
                }

                candidate.SignatureURL = signatureUrl;
            }
            else
            {
                candidate.SignatureURL = originalSignatureUrl;
            }

            candidate.IsSynced = false;

            _commonService._unitOfWork.Repository<Candidate, long>().Update(candidate);

            await _commonService._unitOfWork
                .Repository<CBTCandidatesRecievedData, long>()
                .Query()
                .Where(c => c.NationalId == candidate.NationalId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, CBTCandidateStatus.Updated));

            await _commonService._unitOfWork.Complete();

            return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.Success,
                                   HttpStatusCode.OK,
                                   Resource.CandidateDataUpdatedSuccessfully);
        }

        public async Task<ApiResponse> DeleteCandidateAsync(long candidateId)
        {
            var candidate = await _commonService
                ._unitOfWork
                .Repository<Candidate, long>()
                .GetObjAsync(result => result.Id == candidateId, Including: nameof(Candidate.CandidateOrganizationNodeLookupItems));

            if (candidate is null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.CandidateNotFound);
            }

            _commonService._unitOfWork.Repository<Candidate, long>().SoftDelete(candidate);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.CandidateDeletedSuccessfully);
            }
            else
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.InternalServerError,
                                    Resource.FailedToDeleteCandidate);
            }
        }

        public async Task<ApiResponse> DownloadCandidatesUploadRelatedFilesAsync(GetOrganizationRootResponseDto selectedRoot)
        {
            using var workbook = new XLWorkbook();

            await GenerateCandidatesTemplateSheetAsync(selectedRoot, workbook);

            await GenerateOrganizationLookupsSheetAsync(selectedRoot, workbook);

            if (selectedRoot.ConsiderVenues)
            {
                await GenerateVenuesSheetAsync(workbook);
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files", "Templates");

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            var filePath = Path.Combine(path, "CombinedDumpCandidateTemplate.xlsx");

            const string fileUrl = "/files/Templates/CombinedDumpCandidateTemplate.xlsx";

            workbook.SaveAs(filePath);

            CandidateTemplateDownloadDto result = new()
            {
                CandidatesTemplateFileUrl = fileUrl
            };

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.CombinedTemplateDownloadedSuccessfully,
                                result);
        }

        public async Task<ApiResponse> DownloadCandidatesWithExistingVenuesExcelFileAsync()
        {
            const string fileName = "ImportCandidateWithVenueCode.xlsx";

            var folder = Path.Combine(_env.WebRootPath, "files", "Candidates");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filePath = Path.Combine(folder, fileName);

            using (var workbook = new XLWorkbook())
            {
                var candidateSheet = workbook.Worksheets.Add("CandidatesTemplate");

                candidateSheet.Cell(1, 1).Value = nameof(Candidate.CandidateCode);
                candidateSheet.Cell(1, 2).Value = nameof(Candidate.Name);
                candidateSheet.Cell(1, 3).Value = nameof(Candidate.NationalId);
                candidateSheet.Cell(1, 4).Value = nameof(Candidate.UserName);
                candidateSheet.Cell(1, 5).Value = nameof(Candidate.Password);
                candidateSheet.Cell(1, 6).Value = nameof(Candidate.Qualification);
                candidateSheet.Cell(1, 7).Value = nameof(Candidate.DateOfBirth);
                candidateSheet.Cell(1, 8).Value = nameof(Candidate.Address);
                candidateSheet.Cell(1, 9).Value = nameof(Candidate.Mobile);
                candidateSheet.Cell(1, 10).Value = nameof(Candidate.Email);
                candidateSheet.Cell(1, 11).Value = nameof(Candidate.Gender);
                candidateSheet.Cell(1, 12).Value = nameof(Candidate.RegistrationCenterCode);
                candidateSheet.Cell(1, 13).Value = nameof(Candidate.RegistrationDateTime);
                candidateSheet.Cell(1, 14).Value = nameof(SchedulePaperCandidate.RegistrationNumber);
                candidateSheet.Cell(1, 15).Value = "VenueCode";
                candidateSheet.Cell(1, 16).Value = nameof(SchedulePaperCandidate.CandidateExamDate);

                candidateSheet.Columns().AdjustToContents();

                await GenerateVenuesSheetAsync(workbook);

                workbook.SaveAs(filePath);
            }

            string fileUrl = string.Format("/files/Candidates/{0}", fileName);

            var result = new CandidateTemplateDownloadDto
            {
                CandidatesTemplateFileUrl = fileUrl,
                FileName = fileName
            };

            return _commonService
               ._apiResponse
               .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.GeneratingCandidatesExcelFileWithVenuesIds,
                    result
               );
        }

        public async Task<ExcelFileResult> ExportVerificationCodeExcelAsync(ExportVerificationCodeRequestDto request)
        {
            var examDate = request.ExamDate == ExamDateFilter.Tomorrow
                ? DateTimeHelper.Now.Date.AddDays(1)
                : DateTimeHelper.Now.Date;

            var examDateEnd = examDate.AddDays(1);

            var repo = _commonService._unitOfWork.Repository<SchedulePaperCandidate, long>();

            var query = repo.Query()
                .AsNoTracking()
                .Include(spc => spc.Candidate)
                .Include(spc => spc.Venue)
                .Where(spc =>
                    spc.CandidateExamDate.HasValue &&
                    spc.CandidateExamDate.Value >= examDate &&
                    spc.CandidateExamDate.Value < examDateEnd
                );

            query = query.Where(spc => request.VenueIds.Contains(spc.VenueId));

            var candidates = await query.ToListAsync();

            var dtos = candidates.ConvertAll(c => new VerificationCodeExportDto
            {
                Id = c.Id,
                RegistrationNumber = c.RegistrationNumber.ToString(),
                NationalId = c.Candidate.NationalId,
                VenueName = c.Venue.DisplayName,
                CandidateName = c.Candidate.Name,
                VerificationCode = VerificationCodeHelper.GenerateVerificationCode(c.Candidate.NationalId, c.RegistrationNumber.ToString())
            });

            var columns = new List<ExcelExportHelper.ColumnDefinition<VerificationCodeExportDto>>
            {
                new() { Header = Resource.Id, ValueSelector = x => x.Id },
                new() { Header = Resource.RegistrationNumber, ValueSelector = x => x.RegistrationNumber },
                new() { Header = Resource.NationalId, ValueSelector = x => x.NationalId },
                new() { Header = Resource.VenueName, ValueSelector = x => x.VenueName },
                new() { Header = Resource.CandidateName, ValueSelector = x => x.CandidateName },
                new() { Header = Resource.VerificationCode, ValueSelector = x => x.VerificationCode },
            };

            return new ExcelFileResult(
                Bytes: ExcelExportHelper.GenerateExcelBytes(dtos, Resource.VerificationCodeGenerator, columns),
                FileName: $"VerificationCodes_{DateTimeHelper.Now:yyyy-MM-dd}.xlsx",
                ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            );
        }

        //public async Task<ApiResponse> GetUserCandidateGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Candidate) &&
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

        //public async Task<ApiResponse> GetCandidateGroupsAsync(long candidateId)
        //{
        //    var candidateGroups = await _commonService
        //        ._unitOfWork
        //        .Repository<CandidateGroups, long>()
        //        .GetAllAsync(
        //            x => x.CandidateId == candidateId &&
        //                 !x.OESGroup.IsTemplate,
        //            Including: nameof(CandidateGroups.OESGroup)
        //        );

        //    var candidateGroupsDto = new CandidateGroupDto();

        //    candidateGroups.ToList().ForEach(cg =>
        //        candidateGroupsDto.GroupsIds.Add(cg.OESGroupId));

        //    candidateGroupsDto.OwnerGroupId = candidateGroups
        //        .FirstOrDefault(x =>
        //            x.OESGroup != null &&
        //            x.OESGroup.AutoCreatedForUser
        //        )?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(
        //        CustomCodeStatus.Success,
        //        HttpStatusCode.OK,
        //        Resource.SuccessfulFetching,
        //        candidateGroupsDto
        //    );
        //}

        public async Task<ApiResponse> AddMultipleCandidatesCaller(CandidateAndLookUpsResponseDto candidateAndLookUpsResponseDto)
        {
            if (candidateAndLookUpsResponseDto.File == null || candidateAndLookUpsResponseDto.File.Length == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.BadRequest,
                    Resource.NoFileUploadedOrEmpty
                );
            }

            var processingReturns = await _candidateFileProcessingService.ProcessFileAsync(candidateAndLookUpsResponseDto.File);

            if (processingReturns.StatusCode != HttpStatusCode.OK)
                return processingReturns;

            var candidatesKey = Guid.NewGuid();

            _memoryCacheRepository.SetItemInCache(candidatesKey, (List<AddMultipleCandidateDto>)processingReturns.Data);

            BackgroundJob.Enqueue(() => ProcessCandidateImportAsync(candidatesKey, _filterParamsValues));

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesJobHasBeenScheduledSuccessfully
            );
        }

        public async Task<ApiResponse> AddMultipleCandidatesToSchedulePaperCaller(CandidateAndLookUpsAndSchedulePapersResponseDto candidateAndLookUpsAndSchedulePapersResponse)
        {
            if (candidateAndLookUpsAndSchedulePapersResponse.File == null || candidateAndLookUpsAndSchedulePapersResponse.File.Length == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.BadRequest,
                    Resource.NoFileUploadedOrEmpty
                );
            }

            var candidates = new List<AddMultipleCandidateDto>();

            await foreach (var candidate in _candidateFileProcessingService.ProcessWithoutValidation(candidateAndLookUpsAndSchedulePapersResponse.File))
            {
                candidates.Add(candidate);
            }

            if (candidates == null || candidates.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.BadRequest,
                    Resource.NoCandidatesFoundInFile
                );
            }

            var addBatchResponse = await _candidateBatchImportHistoryService.AddBatchAsync(new AddCandidateBatchImportHistoryRequestDto
            {
                File = candidateAndLookUpsAndSchedulePapersResponse.File,
                SchedulePaperId = candidateAndLookUpsAndSchedulePapersResponse.SchedulePaperId
            });

            if (addBatchResponse.StatusCode != HttpStatusCode.OK)
            {
                return _commonService._apiResponse.GetApiResponse(
                   CustomCodeStatus.InternalServerError,
                   HttpStatusCode.InternalServerError,
                   addBatchResponse.Message
                );
            }

            var candidateBatchImportHistory = (CandidateBatchImportHistory)addBatchResponse.Data;

            var candidatesKey = Guid.NewGuid();

            _memoryCacheRepository.SetItemInCache(candidatesKey, candidates);

            FilterParamsValues filterParamsValues = new FilterParamsValues
            {
                OrganizationId = _filterParamsValues.OrganizationId,
                Signature = _filterParamsValues.Signature,
                UserId = _filterParamsValues.UserId,
                UserEmail = _filterParamsValues.UserEmail,
            };

            BackgroundJob.Enqueue(() => ProcessCandidateImportToSchedulePaperAsync(
                candidatesKey,
                candidateAndLookUpsAndSchedulePapersResponse.SchedulePaperId,
                candidateAndLookUpsAndSchedulePapersResponse.VenueId,
                candidateBatchImportHistory.Id,
                filterParamsValues
            ));

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesJobHasBeenScheduledSuccessfully
            );
        }

        public async Task<ApiResponse> DumpImportCandidatesWithSchedulePaperCaller(CandidatesAndSchedulePaperResponseDto candidatesAndSchedulePaperResponseDto)
        {
            if (candidatesAndSchedulePaperResponseDto.File == null || candidatesAndSchedulePaperResponseDto.File.Length == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.BadRequest,
                    Resource.NoFileUploaded
                );
            }

            var candidates = new List<DumpImportCandidateRequestDto>();

            await foreach (var can in _importToScheduleFileProcessingService.ProcessWithoutValidation(candidatesAndSchedulePaperResponseDto.File))
            {
                candidates.Add(can);
            }

            var uniqueVenueCodesFromExcel = ExtractUniqueVenueCodesFromCandidates(candidates);

            var scheduleVenues = await GetValidatedVenuesAsync(candidatesAndSchedulePaperResponseDto.SchedulePaperId, uniqueVenueCodesFromExcel);

            var validationResult = await ValidateVenueIdentifiersAsync(candidates, scheduleVenues);

            var addBatchResponse = await _candidateBatchImportHistoryService.AddBatchAsync(new AddCandidateBatchImportHistoryRequestDto
            {
                File = candidatesAndSchedulePaperResponseDto.File,
                SchedulePaperId = candidatesAndSchedulePaperResponseDto.SchedulePaperId
            });

            if (addBatchResponse.StatusCode != HttpStatusCode.OK)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    addBatchResponse.Message
                );
            }

            var candidateBatchImportHistory = (CandidateBatchImportHistory)addBatchResponse.Data;

            var candidatesKey = Guid.NewGuid();

            _memoryCacheRepository.SetItemInCache(candidatesKey, candidates);

            FilterParamsValues filterParamsValues = new FilterParamsValues
            {
                OrganizationId = _filterParamsValues.OrganizationId,
                Signature = _filterParamsValues.Signature,
                UserId = _filterParamsValues.UserId,
                UserEmail = _filterParamsValues.UserEmail,
            };

            BackgroundJob.Enqueue(() => ProcessDumpCandidateImportWithSchedulePaperAsync(
                candidatesKey,
                validationResult.CandidateVenueLinks,
                filterParamsValues,
                candidatesAndSchedulePaperResponseDto.SchedulePaperId,
                candidateBatchImportHistory.Id
            ));

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.DumpCandidatesJobScheduledSuccessfully
            );
        }

        public async Task<ApiResponse> AllocateCandidateByLookUpsIdsCaller(AllocateLookUpsSchedulePaperRequestDto allocateLookUpsSchedulePaperRequestDto)
        {
            if (allocateLookUpsSchedulePaperRequestDto.LookUpIds.Length == 0 || allocateLookUpsSchedulePaperRequestDto.LookUpIds == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.LookUpIdCannotBeEmpty);
            }

            if (allocateLookUpsSchedulePaperRequestDto.SchedulePaperId == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.BadRequest,
                                    Resource.SchedulePaperIdCannotBeZero);
            }

            BackgroundJob.Enqueue(() => ProcessAllocateCandidatesAsync(allocateLookUpsSchedulePaperRequestDto, _filterParamsValues));

            await Task.CompletedTask;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesJobScheduledSuccessfully
            );
        }


        #region Get Schedule Venues

        private static List<string> ExtractUniqueVenueCodesFromCandidates(List<DumpImportCandidateRequestDto> candidates)
        {
            return [.. candidates
                .Select(c => c.VenueCode?.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
            ];
        }

        public async Task<List<VenueValidationDto>> GetValidatedVenuesAsync(long schedulePaperId, List<string> excelVenueCodes)
        {
            var schedulePaperRepository = _commonService._unitOfWork.Repository<SchedulePaper, long>();
            var venueRepository = _commonService._unitOfWork.Repository<Venue, long>();
            var scheduleVenueRepository = _commonService._unitOfWork.Repository<ScheduleVenue, long>();

            var scheduleId = await schedulePaperRepository
                .Query()
                .AsNoTracking()
                .Where(sp => sp.Id == schedulePaperId)
                .Select(sp => sp.ScheduleMetadataId)
                .FirstOrDefaultAsync();

            if (scheduleId == 0)
            {
                return [];
            }

            var linkedVenueIds = (await scheduleVenueRepository
                .Query()
                .AsNoTracking()
                .Where(sv => sv.ScheduleId == scheduleId)
                .Select(sv => sv.VenueId)
                .ToListAsync())
                .ToHashSet();

            var scheduleHasAnyLinkedVenues = linkedVenueIds.Any();

            var validatedVenues = await venueRepository
                .Query()
                .AsNoTracking()
                .Where(v => excelVenueCodes.Contains(v.Code.Trim()))
                .Select(v => new VenueValidationDto
                {
                    VenueId = v.Id,
                    VenueCode = v.Code.Trim(),
                    IsExistingInDb = true,
                    IsLinkedToSchedule = !scheduleHasAnyLinkedVenues || linkedVenueIds.Contains(v.Id)
                })
                .ToListAsync();

            return validatedVenues;
        }

        #endregion

        #region Helper Methods

        private async Task<string> UploadFileToDocLib(IFormFile file)
        {
            await using var memoryStream = new MemoryStream();

            await file.CopyToAsync(memoryStream);

            var fileContent = memoryStream.ToArray();

            var imageData = new MediaFileDataDto
            {
                Name = $"{Guid.NewGuid()}{file.FileName}",

                Type = file.ContentType,

                Size = file.Length,

                Content = fileContent
            };

            var response = await _docLibBackEndHttpClient.PostAsJsonAsync<MediaFileDataDto, NewApiResponse<DocumentUrlFileResponseDto>>("Document/UploadDocumentContent", imageData);

            if (response.Success)
            {
                return $"{CentralizedUrlHelper.DocLibApiBaseUrl}{response.Data.FileRelativeUrl}";
            }

            return null;
        }

        private async Task DeleteFileFromDocLib(Guid documentId)
        {
            await _docLibBackEndHttpClient.HttpClient.DeleteAsync($"/api/Document/Delete?documentId={documentId}");
        }

        private static string GetGenderPredefinedImage(int gender)
        {
            const string maleURL = "male-img.jpg";

            const string femaleURL = "female-img.jpg";

            if (gender == 0)
                return maleURL;
            else
                return femaleURL;
        }

        private static string GetSignaturePredefinedImage() => "signature-img.png";

        private static async Task<(List<ExcelValidationErrorDto> InvalidVenues, List<CandidateVenueLinkTempDto> CandidateVenueLinks)> ValidateVenueIdentifiersAsync(List<DumpImportCandidateRequestDto> candidates, List<VenueValidationDto> venueValidationData)
        {
            var invalidVenues = new List<ExcelValidationErrorDto>();

            var candidateVenueLinks = new List<CandidateVenueLinkTempDto>();

            int currentOriginalRow = 1;

            var registrationNumbers = new HashSet<string>();

            foreach (var dumpCandidate in candidates)
            {
                currentOriginalRow++;

                var venueCode = dumpCandidate.VenueCode?.Trim();

                long venueId = 0;

                bool isVenueCodeValid = true;

                string validationError = null;

                var venueStatus = venueValidationData.FirstOrDefault(v => string.Equals(v.VenueCode, venueCode));

                if (venueStatus == null || !venueStatus.IsExistingInDb)
                {
                    isVenueCodeValid = false;
                    validationError = string.Format(Resource.VenueCodeDoesNotExist, dumpCandidate.VenueCode);
                }
                else if (!venueStatus.IsLinkedToSchedule)
                {
                    isVenueCodeValid = false;
                    validationError = string.Format(Resource.VenueCodeExistsButNotLinkedToSchedule, dumpCandidate.VenueCode);
                }
                else
                {
                    venueId = venueStatus.VenueId;
                }

                if (!isVenueCodeValid && validationError != null)
                {
                    invalidVenues.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = currentOriginalRow,
                        CandidateIdentifier = dumpCandidate.Email,
                        FieldName = nameof(dumpCandidate.VenueCode),
                        Value = dumpCandidate.VenueCode,
                        ErrorMessage = validationError
                    });
                }

                if (string.IsNullOrEmpty(dumpCandidate.RegistrationNumber))
                {
                    invalidVenues.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = currentOriginalRow,
                        CandidateIdentifier = dumpCandidate.Email,
                        FieldName = nameof(dumpCandidate.RegistrationNumber),
                        Value = dumpCandidate.RegistrationNumber,
                        ErrorMessage = Resource.RegistrationNumberIsRequired
                    });
                }
                else if (!registrationNumbers.Add(dumpCandidate.RegistrationNumber))
                {
                    invalidVenues.Add(new ExcelValidationErrorDto
                    {
                        RowNumber = currentOriginalRow,
                        CandidateIdentifier = dumpCandidate.Email,
                        FieldName = nameof(dumpCandidate.RegistrationNumber),
                        Value = dumpCandidate.RegistrationNumber,
                        ErrorMessage = Resource.DuplicateRegistrationNumber
                    });
                }

                if (isVenueCodeValid)
                {
                    candidateVenueLinks.Add(new CandidateVenueLinkTempDto
                    {
                        CandidateEmail = dumpCandidate.Email,
                        VenueCode = venueCode,
                        VenueId = venueId,
                        RegistrationNumber = dumpCandidate.RegistrationNumber,
                        CandidateExamDate = DateTime.TryParse(dumpCandidate.CandidateExamDate, out var examDate) ? examDate : null
                    });
                }
            }

            await Task.CompletedTask;

            return (invalidVenues, candidateVenueLinks);
        }

        private ApiResponse BuildInvalidLookupsResponse(List<InvalidLookupErrorDto> invalidLookups)
        {
            const int ErrorThreshold = 100;

            if (invalidLookups?.Count > 0)
            {
                var validationErrors = invalidLookups.Take(ErrorThreshold).Select(e => new ExcelValidationErrorDto
                {
                    RowNumber = e.OriginalRowNumber,
                    CandidateIdentifier = e.CandidateEmail,
                    FieldName = e.ExcelColumnHeader,
                    Value = e.InvalidLookupValue,
                    ErrorMessage = e.Reason
                }).ToList();

                int emptyFieldCount = validationErrors
                    .Where(e => e.ErrorMessage.Contains("empty", StringComparison.OrdinalIgnoreCase) ||
                                e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.RowNumber)
                    .Distinct()
                    .Count();

                int invalidIdCount = validationErrors
                    .Where(e => e.ErrorMessage.Contains("not a valid number", StringComparison.OrdinalIgnoreCase) ||
                                e.ErrorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                                e.ErrorMessage.Contains("does not belong", StringComparison.OrdinalIgnoreCase))
                    .Select(e => e.RowNumber)
                    .Distinct()
                    .Count();

                string mainErrorMessage = string.Empty;

                if (emptyFieldCount > 0 && invalidIdCount > 0)
                {
                    mainErrorMessage = string.Format(Resource.FileHasEmptyAndInvalidLookupRows, emptyFieldCount, invalidIdCount);
                }
                else if (emptyFieldCount > 0)
                {
                    mainErrorMessage = string.Format(Resource.FileHasEmptyLookupRows, emptyFieldCount);
                }
                else if (invalidIdCount > 0)
                {
                    mainErrorMessage = string.Format(Resource.FileHasInvalidLookupRows, invalidIdCount);
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    mainErrorMessage,
                    validationErrors
                );
            }

            return null;
        }

        private async Task SendCandidateImportSuccessNotificationAsync(FilterParamsValues filterParamsValues, Guid parsedUserId)
        {
            var userExists = await _commonService
                ._unitOfWork
                .Repository<AppUserProfile, Guid>()
                .IsExistAsync(c => c.Id == parsedUserId);

            var notificationSuccessDto = new NotificationDto(
                entity: NotificationEntity.Candidate,
                operation: NotificationOperation.Imported,
                from: Resource.NotificationFromHangfire,
                status: NotificationStatus.Success,
                type: NotificationTypeStatus.Success
            );

            if (userExists)
            {
                var notificationEntity = new Notification
                {
                    Entity = notificationSuccessDto.Entity,
                    Operation = notificationSuccessDto.Operation,
                    From = notificationSuccessDto.From,
                    Status = notificationSuccessDto.Status,
                    Type = notificationSuccessDto.Type,
                    CreationDate = DateTimeHelper.Now,
                    CreationUser = filterParamsValues.UserEmail,
                    OrganizationId = filterParamsValues.OrganizationId,
                    OrganizationSignature = filterParamsValues.Signature
                };

                notificationEntity.AppUserProfileNotifications = new List<NotificationAppUserProfile>
                {
                    new NotificationAppUserProfile
                    {
                        AppUserProfileId = parsedUserId,
                        IsRead = false,
                        Notification = notificationEntity,
                        CreationDate = DateTimeHelper.Now,
                        CreationUser = filterParamsValues.UserEmail,
                        OrganizationId = filterParamsValues.OrganizationId,
                        OrganizationSignature = filterParamsValues.Signature
                    }
                };

                await _commonService._unitOfWork.Repository<Notification, long>().AddAsync(notificationEntity);

                await _commonService._unitOfWork.Complete();
            }

            await _notificationHubService.NotifyAsync(notificationSuccessDto, parsedUserId);
        }

        private async Task SendCandidateAllocateSuccessNotificationAsync(FilterParamsValues filterParamsValues, Guid parsedUserId)
        {
            var userExists = await _commonService
                ._unitOfWork
                .Repository<AppUserProfile, Guid>()
                .IsExistAsync(c => c.Id == parsedUserId);

            var notificationSuccessDto = new NotificationDto(
               entity: NotificationEntity.Candidate,
               operation: NotificationOperation.Imported,
               from: Resource.NotificationFromHangfire,
               status: NotificationStatus.Success,
               type: NotificationTypeStatus.Success
            );

            if (userExists)
            {
                var notificationEntity = new Notification
                {
                    Entity = notificationSuccessDto.Entity,
                    Operation = notificationSuccessDto.Operation,
                    From = notificationSuccessDto.From,
                    Status = notificationSuccessDto.Status,
                    Type = notificationSuccessDto.Type,
                    CreationDate = DateTimeHelper.Now,
                    CreationUser = filterParamsValues.UserEmail,
                    OrganizationId = filterParamsValues.OrganizationId,
                    OrganizationSignature = filterParamsValues.Signature
                };

                notificationEntity.AppUserProfileNotifications = new List<NotificationAppUserProfile>
                {
                    new NotificationAppUserProfile
                    {
                        AppUserProfileId = parsedUserId,
                        IsRead = false,
                        Notification = notificationEntity,
                        CreationDate = DateTimeHelper.Now,
                        CreationUser = filterParamsValues.UserEmail,
                        OrganizationId = filterParamsValues.OrganizationId,
                        OrganizationSignature = filterParamsValues.Signature
                    }
                };

                await _commonService._unitOfWork.Repository<Notification, long>().AddAsync(notificationEntity);

                await _commonService._unitOfWork.Complete();
            }

            await _notificationHubService.NotifyAsync(notificationSuccessDto, parsedUserId);
        }

        private async Task GenerateCandidatesTemplateSheetAsync(GetOrganizationRootResponseDto selectedRoot, XLWorkbook workbook)
        {
            var candidateHeaders = new[]
            {
                nameof(Candidate.CandidateCode),
                nameof(Candidate.Name),
                nameof(Candidate.NationalId),
                nameof(Candidate.UserName),
                nameof(Candidate.Password),
                nameof(Candidate.Qualification),
                nameof(Candidate.DateOfBirth),
                nameof(Candidate.Address),
                nameof(Candidate.Mobile),
                nameof(Candidate.Email),
                nameof(Candidate.Gender),
                nameof(Candidate.RegistrationCenterCode),
                nameof(Candidate.RegistrationDateTime)
            };

            var rootOrganization = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == selectedRoot.Id);

            var organizationStructureNodes = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetAllAsync(x => x.ParentId != null && x.OrganizationStructureSignature == rootOrganization.OrganizationStructureSignature);

            var worksheet = workbook.Worksheets.Add("CandidatesTemplate");

            int colIndex = 1;

            foreach (var header in candidateHeaders)
                worksheet.Cell(1, colIndex++).Value = header;

            foreach (var orgNode in organizationStructureNodes)
                worksheet.Cell(1, colIndex++).Value = $"{orgNode.Name} Id";

            if (selectedRoot.ConsiderVenues)
                worksheet.Cell(1, colIndex).Value = "Venue Id";
        }

        private async Task GenerateOrganizationLookupsSheetAsync(GetOrganizationRootResponseDto selectedRoot, XLWorkbook workbook)
        {
            var rootStructure = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetObjAsync(x => x.Id == selectedRoot.Id);

            var signature = rootStructure.OrganizationStructureSignature;

            var orgStructure = await _commonService
                ._unitOfWork
                .Repository<OrganizationStructure, long>()
                .GetAllAsync(x => x.OrganizationStructureSignature == signature && x.ParentId != null);

            var orderedStructures = orgStructure.OrderBy(x => x.Id).ToList();

            var lookupItems = await _commonService
                ._unitOfWork
                .Repository<OrganizationNodeLookupItem, long>()
                .GetAllAsync(x => orderedStructures.Select(s => s.Id).Contains(x.OrganizationStructureNodeId));

            var lookupDict = lookupItems.ToDictionary(x => x.Id);

            var rows = new List<Dictionary<string, (string Name, long Id)>>();

            var leafNodes = lookupItems
                .Where(l => l.ChildLookupItems == null || !l.ChildLookupItems.Any())
                .ToList();

            foreach (var leaf in leafNodes)
            {
                var DirectoryPAth = new Dictionary<string, (string Name, long Id)>();

                var current = leaf;

                while (current != null)
                {
                    var name = current.OrganizationStructureNode.Name;
                    if (!DirectoryPAth.ContainsKey(name))
                        DirectoryPAth[name] = (current.Name, current.Id);

                    current = current.ParentId.HasValue ? lookupDict.GetValueOrDefault(current.ParentId.Value) : null;
                }

                rows.Add(DirectoryPAth);
            }

            var worksheet = workbook.Worksheets.Add("OrganizationLookups");

            int colIndex = 1;

            var structureNames = orderedStructures.ConvertAll(x => x.Name);

            foreach (var name in structureNames)
            {
                worksheet.Cell(1, colIndex++).Value = name;
                worksheet.Cell(1, colIndex++).Value = $"{name} Id";
            }

            int rowIndex = 2;

            foreach (var row in rows)
            {
                colIndex = 1;
                foreach (var name in structureNames)
                {
                    if (row.TryGetValue(name, out var val))
                    {
                        worksheet.Cell(rowIndex, colIndex++).Value = val.Name;
                        worksheet.Cell(rowIndex, colIndex++).Value = val.Id;
                    }
                    else
                    {
                        worksheet.Cell(rowIndex, colIndex++).Value = string.Empty;
                        worksheet.Cell(rowIndex, colIndex++).Value = string.Empty;
                    }
                }
                rowIndex++;
            }
        }

        private async Task GenerateVenuesSheetAsync(XLWorkbook workbook)
        {
            var venues = await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .GetAllAsync();

            var worksheet = workbook.Worksheets.Add("Venues");

            worksheet.Cell(1, 1).Value = nameof(Venue.Name);
            worksheet.Cell(1, 2).Value = nameof(Venue.Code);
            worksheet.Cell(1, 3).Value = nameof(Venue.Url);

            int row = 2;

            foreach (var venue in venues)
            {
                worksheet.Cell(row, 1).Value = venue.Name;
                worksheet.Cell(row, 2).Value = venue.Code;
                worksheet.Cell(row, 3).Value = venue.Url;
                row++;
            }

            worksheet.Columns().AdjustToContents();
        }

        private async Task<ApiResponse> ValidateCandidateRegistrationAsync(List<DumpImportCandidateRequestDto> candidates, FilterParamsValues filterParamsValues)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var candidatesJson = JsonSerializer.Serialize(candidates, jsonOptions);

            using (var command = _commonService._unitOfWork.CreateDbCommand())
            {
                command.CommandText = "ValidateCandidateRegistration";

                command.CommandType = CommandType.StoredProcedure;

                var inputCandidates = new MySqlParameter("@candidatesJson", MySqlDbType.JSON)
                {
                    Value = candidatesJson
                };

                var inputOrganizationId = new MySqlParameter("@p_OrganizationId", MySqlDbType.Int32)
                {
                    Value = filterParamsValues.OrganizationId
                };

                var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                {
                    Direction = ParameterDirection.Output
                };

                var errorMessageParam = new MySqlParameter("@errorMessage", MySqlDbType.VarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(new[]
                {
                    inputCandidates,
                    inputOrganizationId,
                    returnStatusParam,
                    errorMessageParam
                });

                await _commonService._unitOfWork.OpenConnectionAsync();

                command.CommandTimeout = 300;

                await command.ExecuteNonQueryAsync();

                int returnStatus = Convert.ToInt32(returnStatusParam.Value);

                string errorMessage = Convert.ToString(errorMessageParam.Value);

                if (returnStatus == 0 && !string.IsNullOrWhiteSpace(errorMessage))
                {
                    var errorResponse = new List<ExcelValidationErrorDto>();

                    var candidateRowNumbers = candidates
                        .Select((c, index) => new { Email = c.Email, RowNumber = index + 2 })
                        .GroupBy(x => x.Email)
                        .ToDictionary(g => g.Key, g => g.First().RowNumber);

                    foreach (var entry in errorMessage.Split(',', StringSplitOptions.TrimEntries))
                    {
                        var parts = entry.Split('&', StringSplitOptions.TrimEntries);

                        if (parts.Length == 2)
                        {
                            var email = parts[0];
                            var registrationNumber = parts[1];
                            int? rowNumber = candidateRowNumbers.TryGetValue(email, out var rNum) ? rNum : null;

                            errorResponse.Add(new ExcelValidationErrorDto
                            {
                                CandidateIdentifier = email,
                                ErrorMessage = Resource.DuplicateRegistrationNumber,
                                RowNumber = rowNumber,
                                FieldName = nameof(DumpImportCandidateRequestDto.RegistrationNumber),
                                Value = registrationNumber
                            });
                        }
                    }

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.BadRequest,
                        Resource.Validationhadsomeissues,
                        errorResponse.OrderBy(x => x.RowNumber).ToList()
                    );
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.validationsuccessed
                );
            }
        }

        public async Task<ApiResponse> ValidateCandidateData(CandidateDataCompositeResponseDto candidateDataCompositeResponseDto)
        {
            if (candidateDataCompositeResponseDto.FromDumb)
            {
                if (candidateDataCompositeResponseDto.File == null || candidateDataCompositeResponseDto.File.Length == 0)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.BadRequest,
                        Resource.NoFileUploaded
                    );
                }

                var errors = new CandidateCombinedErrorsResponseDto();

                var ProcessingReturns = await _importToScheduleFileProcessingService.ProcessFileAsync(candidateDataCompositeResponseDto.File);

                if (ProcessingReturns.StatusCode != HttpStatusCode.OK)
                {
                    errors.IsExcelValid = false;

                    errors.ExcelErrorsDto = (List<ExcelValidationErrorDto>)ProcessingReturns.Data;

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.BadRequest,
                        Resource.Error, errors
                    );
                }

                var candidates = (List<DumpImportCandidateRequestDto>)ProcessingReturns.Data;

                var uniqueVenueCodesFromExcel = ExtractUniqueVenueCodesFromCandidates(candidates);

                var scheduleVenues = await GetValidatedVenuesAsync(candidateDataCompositeResponseDto.SchedulePaperId, uniqueVenueCodesFromExcel);

                var validationResult = await ValidateVenueIdentifiersAsync(candidates, scheduleVenues);

                if (validationResult.InvalidVenues.Count > 0)
                {
                    errors.AreVenuesValid = false;

                    errors.ExcelErrorsDto = [.. validationResult.InvalidVenues];

                    return _commonService._apiResponse.GetApiResponse(
                         CustomCodeStatus.ValidationError,
                         HttpStatusCode.InternalServerError,
                         Resource.InvalidVenueCodesProvided,
                         errors
                     );
                }

                var checkExistinceInDatabase = await ValidateCandidateRegistrationAsync(
                    candidates,
                    _filterParamsValues
                );

                if (checkExistinceInDatabase.StatusCode != HttpStatusCode.OK)
                {
                    errors.IsDuplicationValid = false;

                    errors.ExcelErrorsDto = (List<ExcelValidationErrorDto>)checkExistinceInDatabase.Data;

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.InternalServerError,
                        checkExistinceInDatabase.Message,
                        errors
                    );
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.Candidatesvalidated
                );
            }
            else
            {
                if (candidateDataCompositeResponseDto.File == null || candidateDataCompositeResponseDto.File.Length == 0)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.BadRequest,
                        Resource.NoFileUploaded
                    );
                }

                var errors = new CandidateCombinedErrorsResponseDto();

                var processedReturns = await _candidateFileProcessingService.ProcessFileAsync(candidateDataCompositeResponseDto.File);

                if (processedReturns.StatusCode != HttpStatusCode.OK)
                {
                    errors.IsExcelValid = false;

                    errors.ExcelErrorsDto = (List<ExcelValidationErrorDto>)processedReturns.Data;

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.BadRequest,
                        processedReturns.Message,
                        errors
                    );
                }

                var simpleCandidates = (List<AddMultipleCandidateDto>)processedReturns.Data;

                var candidatesForValidation = simpleCandidates.ConvertAll(c => new DumpImportCandidateRequestDto
                {
                    CandidateCode = c.CandidateCode,
                    Name = c.Name,
                    UserName = c.UserName,
                    NationalId = c.NationalId,
                    Password = c.Password,
                    Qualification = c.Qualification,
                    DateOfBirth = c.DateOfBirth,
                    Address = c.Address,
                    Mobile = c.Mobile,
                    Email = c.Email,
                    Gender = c.Gender,
                    RegistrationCenterCode = c.RegistrationCenterCode,
                    RegistrationDateTime = c.RegistrationDateTime,
                    PhotoURL = c.PhotoURL,
                    SignatureURL = c.SignatureURL,
                    RegistrationNumber = c.RegistrationNumber,
                    VenueCode = string.Empty
                });

                var checkExistinceInDatabase = await ValidateCandidateRegistrationAsync(
                    candidatesForValidation,
                    _filterParamsValues
                );

                if (checkExistinceInDatabase.StatusCode != HttpStatusCode.OK)
                {
                    errors.IsDuplicationValid = false;

                    errors.ExcelErrorsDto = (List<ExcelValidationErrorDto>)checkExistinceInDatabase.Data;

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.InternalServerError,
                        HttpStatusCode.InternalServerError,
                        checkExistinceInDatabase.Message,
                        errors
                    );
                }

                return _commonService._apiResponse.GetApiResponse(
                   CustomCodeStatus.Success,
                   HttpStatusCode.OK,
                   Resource.Candidatesvalidated
                );
            }
        }

        #endregion Helper Methods


        #region Hangfire Methods

        [JobDisplayName("Oes System - Candidate Import"), AutomaticRetry(Attempts = 0)]
        public async Task ProcessCandidateImportAsync(Guid candidatesKey, FilterParamsValues filterParamsValues)
        {
            var parsedUserId = Guid.Parse(filterParamsValues.UserId);

            var candidateJson = _memoryCacheRepository.GetItemFromCache<List<AddMultipleCandidateDto>, Guid>(candidatesKey, true);

            foreach (var candidate in candidateJson)
            {
                candidate.PhotoURL = GetGenderPredefinedImage(candidate.Gender);
                candidate.SignatureURL = GetSignaturePredefinedImage();
            }

            List<Candidate> candidatesToAdd = _commonService._mapper.Map<List<Candidate>>(candidateJson);

            const int BATCH_SIZE = 5000;
            var errors = new List<string>();
            int successfulBatches = 0;
            int totalBatches = (int)Math.Ceiling((double)candidatesToAdd.Count / BATCH_SIZE);

            for (int i = 0; i < candidatesToAdd.Count; i += BATCH_SIZE)
            {
                int currentBatchNumber = (i / BATCH_SIZE) + 1;

                var currentBatchCandidates = candidatesToAdd.Skip(i).Take(BATCH_SIZE).ToList();
                var candidatesJson = JsonSerializer.Serialize(currentBatchCandidates, new JsonSerializerOptions
                {
                    DefaultBufferSize = 65536,
                    MaxDepth = 10
                });

                using (var command = _commonService._unitOfWork.CreateDbCommand())
                {
                    command.CommandText = "BulkInsertCandidatesAndLookUps";
                    command.CommandType = CommandType.StoredProcedure;

                    var inputCandidates = new MySqlParameter("@candidatesJson", MySqlDbType.JSON)
                    {
                        Value = candidatesJson
                    };
                    var inputOrgSignature = new MySqlParameter("@OrgSignature", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.Signature
                    };
                    var inputCreationUser = new MySqlParameter("@CreationUser", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.UserEmail
                    };
                    var inputOrganizationId = new MySqlParameter("@OrganizationId", MySqlDbType.Int32)
                    {
                        Value = filterParamsValues.OrganizationId
                    };
                    var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    var errorMessageParam = new MySqlParameter("@errorMessage", MySqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output
                    };

                    command.Parameters.AddRange(new[]
                    {
                        inputCandidates,
                        inputCreationUser,
                        inputOrgSignature,
                        inputOrganizationId,
                        returnStatusParam,
                        errorMessageParam
                    });

                    await _commonService._unitOfWork.OpenConnectionAsync();
                    command.CommandTimeout = 300;
                    await command.ExecuteNonQueryAsync();

                    int returnStatus = Convert.ToInt32(returnStatusParam.Value);
                    string errorMessage = Convert.ToString(errorMessageParam.Value);

                    if (returnStatus == 0)
                    {
                        errors.Add($"Batch {currentBatchNumber}/{totalBatches}: {errorMessage}");
                    }
                    else
                    {
                        successfulBatches++;
                    }
                }
            }

            if (errors.Count > 0)
            {
                if (successfulBatches == 0)
                {
                    var notificationErrorsDto = new NotificationDto(
                       entity: NotificationEntity.Candidate,
                       operation: NotificationOperation.Imported,
                       from: Resource.NotificationFromHangfire,
                       status: NotificationStatus.Failed,
                       type: NotificationTypeStatus.Error,
                       parameterName: $"{string.Join("; ", errors)}: {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorsDto, parsedUserId);
                }
                else
                {
                    var notificationErrorsDto = new NotificationDto(
                         entity: NotificationEntity.Candidate,
                         operation: NotificationOperation.Imported,
                         from: Resource.NotificationFromHangfire,
                         status: NotificationStatus.Success,
                         type: NotificationTypeStatus.Success,
                         parameterName: $"{string.Join("; ", errors)}: ({successfulBatches} / {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorsDto, parsedUserId);
                }
            }
            else
            {
                await SendCandidateImportSuccessNotificationAsync(filterParamsValues, parsedUserId);
            }
        }

        [JobDisplayName("Oes System - Dump Candidate Import"), AutomaticRetry(Attempts = 0)]
        public async Task ProcessDumpCandidateImportAsync(Guid candidatesKey, List<CandidateLookupLinkTempDto> allCandidateLookupLinksForDb, FilterParamsValues filterParamsValues)
        {
            var parsedUserId = Guid.Parse(filterParamsValues.UserId);

            var candidateJson = _memoryCacheRepository.GetItemFromCache<List<DumpImportCandidateRequestDto>, Guid>(candidatesKey, true);

            foreach (var candidate in candidateJson)
            {
                candidate.PhotoURL = GetGenderPredefinedImage(candidate.Gender);
                candidate.SignatureURL = GetSignaturePredefinedImage();
            }

            List<Candidate> candidatesToAdd = _commonService._mapper.Map<List<Candidate>>(candidateJson);

            const int BATCH_SIZE = 5000;
            var errors = new List<string>();
            int successfulBatches = 0;
            int totalBatches = (int)Math.Ceiling((double)candidatesToAdd.Count / BATCH_SIZE);

            for (int i = 0; i < candidatesToAdd.Count; i += BATCH_SIZE)
            {
                var currentBatchCandidates = candidatesToAdd.Skip(i).Take(BATCH_SIZE).ToList();
                var currentBatchCandidateEmails = new HashSet<string>(currentBatchCandidates.Select(c => c.Email));
                var currentBatchLookupLinks = allCandidateLookupLinksForDb
                    .Where(link => currentBatchCandidateEmails.Contains(link.CandidateEmail))
                    .ToList();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var candidatesJson = JsonSerializer.Serialize(currentBatchCandidates, jsonOptions);

                var candidateLookupLinksJson = JsonSerializer.Serialize(currentBatchLookupLinks, jsonOptions);

                using (var command = _commonService._unitOfWork.CreateDbCommand())
                {
                    command.CommandText = "BulkInsertCandidatesAndAssociatedLookups";
                    command.CommandType = CommandType.StoredProcedure;

                    var inputCandidates = new MySqlParameter("@candidatesJson", MySqlDbType.JSON)
                    {
                        Value = candidatesJson
                    };
                    var inputCandidatesLookupLinks = new MySqlParameter("@candidatesLookupLinksJson", MySqlDbType.JSON)
                    {
                        Value = candidateLookupLinksJson
                    };
                    var inputOrgSignature = new MySqlParameter("@OrgSignature", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.Signature
                    };
                    var inputCreationUser = new MySqlParameter("@CreationUser", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.UserEmail
                    };
                    var inputOrganizationId = new MySqlParameter("@OrganizationId", MySqlDbType.Int32)
                    {
                        Value = filterParamsValues.OrganizationId
                    };
                    var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    var errorMessageParam = new MySqlParameter("@errorMessage", MySqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output
                    };

                    command.Parameters.AddRange(new[]
                    {
                        inputCandidates,
                        inputCandidatesLookupLinks,
                        inputCreationUser,
                        inputOrgSignature,
                        inputOrganizationId,
                        returnStatusParam,
                        errorMessageParam
                    });

                    await _commonService._unitOfWork.OpenConnectionAsync();
                    command.CommandTimeout = 300;
                    await command.ExecuteNonQueryAsync();

                    int returnStatus = Convert.ToInt32(returnStatusParam.Value);
                    string errorMessage = Convert.ToString(errorMessageParam.Value);

                    if (returnStatus == 0)
                    {
                        errors.Add($"Batch {currentBatchCandidates}/{totalBatches}: {errorMessage}");
                    }
                    else
                    {
                        successfulBatches++;
                    }
                }
            }

            if (errors.Count > 0)
            {
                if (successfulBatches == 0)
                {
                    var notificationErrorsDto = new NotificationDto(
                         entity: NotificationEntity.Candidate,
                         operation: NotificationOperation.Imported,
                         from: Resource.NotificationFromHangfire,
                         status: NotificationStatus.Failed,
                         type: NotificationTypeStatus.Error,
                         parameterName: $"{string.Join("; ", errors)}: {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorsDto, parsedUserId);
                }
                else
                {
                    var notificationErrorDto = new NotificationDto(
                        entity: NotificationEntity.Candidate,
                        operation: NotificationOperation.Imported,
                        from: Resource.NotificationFromHangfire,
                        status: NotificationStatus.Success,
                        type: NotificationTypeStatus.Success,
                        parameterName: $"{string.Join("; ", errors)}: {successfulBatches} / {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorDto, parsedUserId);
                }
            }
            else
            {
                await SendCandidateImportSuccessNotificationAsync(filterParamsValues, parsedUserId);
            }
        }

        [JobDisplayName("Oes System - Dump Candidate Import With Schedule Paper"), AutomaticRetry(Attempts = 0)]
        public async Task ProcessDumpCandidateImportWithSchedulePaperAsync(Guid candidatesKey, List<CandidateVenueLinkTempDto> allCandidateVenueLinksForDb, FilterParamsValues filterParamsValues, long schedulePaperId, long batchId)
        {
            var parsedUserId = Guid.TryParse(filterParamsValues.UserId, out var userId) ? userId : Guid.Empty;

            var candidates = _memoryCacheRepository.GetItemFromCache<List<DumpImportCandidateRequestDto>, Guid>(candidatesKey, true);

            if (candidates == null || candidates.Count == 0)
            {
                var notificationErrorDto = new NotificationDto(
                    entity: NotificationEntity.Candidate,
                    operation: NotificationOperation.Imported,
                    from: Resource.NotificationFromHangfire,
                    status: NotificationStatus.Failed,
                    type: NotificationTypeStatus.Error,
                    parameterName: "No candidates found in cache - cache may have expired"
                );

                await _notificationHubService.NotifyAsync(notificationErrorDto, parsedUserId);

                return;
            }

            allCandidateVenueLinksForDb ??= [];

            foreach (var candidate in candidates)
            {
                candidate.PhotoURL = GetGenderPredefinedImage(candidate.Gender);
                candidate.SignatureURL = GetSignaturePredefinedImage();
            }

            const int BATCH_SIZE = 5000;
            var errors = new List<string>();
            int successfulBatches = 0;
            int totalBatches = (int)Math.Ceiling((double)candidates.Count / BATCH_SIZE);

            for (int i = 0; i < candidates.Count; i += BATCH_SIZE)
            {
                var currentBatchCandidates = candidates.Skip(i).Take(BATCH_SIZE).ToList();
                var currentBatchCandidateEmails = new HashSet<string>(currentBatchCandidates.Where(c => !string.IsNullOrEmpty(c.Email)).Select(c => c.Email!));
                var currentBatchVenueLinks = allCandidateVenueLinksForDb
                    .Where(link => link.CandidateEmail != null && currentBatchCandidateEmails.Contains(link.CandidateEmail))
                    .ToList();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var candidatesJson = JsonSerializer.Serialize(currentBatchCandidates, jsonOptions);

                var candidateVenueLinksJson = JsonSerializer.Serialize(currentBatchVenueLinks, jsonOptions);

                await using (var command = _commonService._unitOfWork.CreateDbCommand())
                {
                    command.CommandText = "BulkInsertCandidatesAndAssociatedVenuesWithSchedulePaper";
                    command.CommandType = CommandType.StoredProcedure;

                    var inputCandidates = new MySqlParameter("@candidatesJson", MySqlDbType.JSON)
                    {
                        Value = candidatesJson
                    };
                    var inputcandidateVenueLinks = new MySqlParameter("@candidateVenueLinksJson", MySqlDbType.JSON)
                    {
                        Value = candidateVenueLinksJson
                    };
                    var inputSchedulePaperId = new MySqlParameter("@SchedulePaperId", MySqlDbType.Int32)
                    {
                        Value = (int)schedulePaperId
                    };
                    var inputOrgSignature = new MySqlParameter("@OrgSignature", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.Signature ?? ""
                    };
                    var inputCreationUser = new MySqlParameter("@CreationUser", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.UserEmail ?? ""
                    };
                    var inputOrganizationId = new MySqlParameter("@OrganizationId", MySqlDbType.Int32)
                    {
                        Value = filterParamsValues.OrganizationId
                    };
                    var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    var inputBatchId = new MySqlParameter("@BatchId", MySqlDbType.Int32)
                    {
                        Value = (int)batchId
                    };
                    var errorMessageParam = new MySqlParameter("@errorMessage", MySqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output
                    };

                    command.Parameters.AddRange(new[]
                    {
                        inputCandidates,
                        inputcandidateVenueLinks,
                        inputSchedulePaperId,
                        inputOrgSignature,
                        inputCreationUser,
                        inputBatchId,
                        inputOrganizationId,
                        returnStatusParam,
                        errorMessageParam
                    });

                    await _commonService._unitOfWork.OpenConnectionAsync();
                    command.CommandTimeout = 300;

                    await command.ExecuteNonQueryAsync();

                    int returnStatus = Convert.ToInt32(returnStatusParam.Value);
                    string errorMessage = Convert.ToString(errorMessageParam.Value);
                    int currentBatchNumber = (i / BATCH_SIZE) + 1;

                    if (returnStatus == 0)
                    {
                        errors.Add($"Batch {currentBatchNumber}/{totalBatches}: {errorMessage}");
                    }
                    else
                    {
                        successfulBatches++;
                    }
                }
            }

            if (errors.Count > 0)
            {
                var targetBatch = await _commonService
                    ._unitOfWork
                    .Repository<CandidateBatchImportHistory, long>()
                    .GetObjAsync(x => x.Id == batchId);

                _commonService._unitOfWork.Repository<CandidateBatchImportHistory, long>().Delete(targetBatch);

                if (successfulBatches == 0)
                {
                    var notificationErrorsDto = new NotificationDto(
                        entity: NotificationEntity.Candidate,
                        operation: NotificationOperation.Imported,
                        from: Resource.NotificationFromHangfire,
                        status: NotificationStatus.Failed,
                        type: NotificationTypeStatus.Error,
                        parameterName: $"{string.Join("; ", errors)}: {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorsDto, parsedUserId);
                }
                else
                {
                    var notificationErrorsDto = new NotificationDto(
                        entity: NotificationEntity.Candidate,
                        operation: NotificationOperation.Imported,
                        from: Resource.NotificationFromHangfire,
                        status: NotificationStatus.Success,
                        type: NotificationTypeStatus.Success,
                        parameterName: $"{string.Join("; ", errors)}: ({successfulBatches} / {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorsDto, parsedUserId);
                }
            }
            else
            {
                await SendCandidateImportSuccessNotificationAsync(filterParamsValues, parsedUserId);
            }
        }

        [JobDisplayName("Oes System - Candidate Import To Schedule Paper"), AutomaticRetry(Attempts = 0)]
        public async Task ProcessCandidateImportToSchedulePaperAsync(Guid candidatesKey, long SchedulePaperId, long VenueId, long batchId, FilterParamsValues filterParamsValues)
        {
            var parsedUserId = Guid.Parse(filterParamsValues.UserId);

            var candidatesToAdd = _memoryCacheRepository.GetItemFromCache<List<AddMultipleCandidateDto>, Guid>(candidatesKey, true);

            if (candidatesToAdd == null || candidatesToAdd.Count == 0)
            {
                var notificationErrorDto = new NotificationDto(
                    entity: NotificationEntity.Candidate,
                    operation: NotificationOperation.Imported,
                    from: Resource.NotificationFromHangfire,
                    status: NotificationStatus.Failed,
                    type: NotificationTypeStatus.Error,
                    parameterName: "No candidates found in cache - cache may have expired"
                );

                await _notificationHubService.NotifyAsync(notificationErrorDto, parsedUserId);

                return;
            }

            foreach (var candidate in candidatesToAdd)
            {
                candidate.PhotoURL = GetGenderPredefinedImage(candidate.Gender);
                candidate.SignatureURL = GetSignaturePredefinedImage();

                if (!string.IsNullOrWhiteSpace(candidate.CandidateExamDate) && DateTime.TryParse(candidate.CandidateExamDate, out var parsedExamDate))
                {
                    candidate.CandidateExamDate = parsedExamDate.ToString("yyyy-MM-ddTHH:mm:ss");
                }
            }

            const int BATCH_SIZE = 5000;
            var errors = new List<string>();
            int successfulBatches = 0;
            int totalBatches = (int)Math.Ceiling((double)candidatesToAdd.Count / BATCH_SIZE);

            for (int i = 0; i < candidatesToAdd.Count; i += BATCH_SIZE)
            {
                var currentBatchCandidates = candidatesToAdd.Skip(i).Take(BATCH_SIZE).ToList();
                var candidatesJson = JsonSerializer.Serialize(currentBatchCandidates, new JsonSerializerOptions
                {
                    DefaultBufferSize = 65536,
                    MaxDepth = 10
                });

                await using (var command = _commonService._unitOfWork.CreateDbCommand())
                {
                    command.CommandText = "BulkInsertCandidatesandSchedulePapers";
                    command.CommandType = CommandType.StoredProcedure;

                    var inputCandidates = new MySqlParameter("@candidatesJson", MySqlDbType.JSON)
                    {
                        Value = candidatesJson
                    };
                    var inputVenueId = new MySqlParameter("@VenueId", MySqlDbType.Int64)
                    {
                        Value = VenueId
                    };
                    var inputSchedulePaperId = new MySqlParameter("@SchedulePaperId", MySqlDbType.Int64)
                    {
                        Value = SchedulePaperId
                    };
                    var inputOrgSignature = new MySqlParameter("@OrgSignature", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.Signature
                    };
                    var inputCreationUser = new MySqlParameter("@CreationUser", MySqlDbType.VarChar, 255)
                    {
                        Value = filterParamsValues.UserEmail
                    };
                    var inputOrganizationId = new MySqlParameter("@OrganizationId", MySqlDbType.Int32)
                    {
                        Value = filterParamsValues.OrganizationId
                    };
                    var inputBatchId = new MySqlParameter("@BatchId", MySqlDbType.Int64)
                    {
                        Value = batchId
                    };
                    var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                    {
                        Direction = ParameterDirection.Output
                    };
                    var errorMessageParam = new MySqlParameter("@errorMessage", MySqlDbType.VarChar, 255)
                    {
                        Direction = ParameterDirection.Output
                    };

                    command.Parameters.AddRange(new[]
                    {
                        inputCandidates,
                        inputCreationUser,
                        inputSchedulePaperId,
                        inputVenueId,
                        inputBatchId,
                        inputOrgSignature,
                        inputOrganizationId,
                        returnStatusParam,
                        errorMessageParam
                    });

                    await _commonService._unitOfWork.OpenConnectionAsync();
                    command.CommandTimeout = 300;
                    await command.ExecuteNonQueryAsync();

                    int returnStatus = Convert.ToInt32(returnStatusParam.Value);
                    string errorMessage = Convert.ToString(errorMessageParam.Value);
                    int currentBatchNumber = (i / BATCH_SIZE) + 1;

                    if (returnStatus == 0)
                    {
                        errors.Add($"Batch {currentBatchNumber}/{totalBatches}: {errorMessage}");
                    }
                    else
                    {
                        successfulBatches++;
                    }
                }
            }

            if (errors.Count > 0)
            {
                var targetBatch = await _commonService._unitOfWork.Repository<CandidateBatchImportHistory, long>().GetObjAsync(x => x.Id == batchId);

                _commonService._unitOfWork.Repository<CandidateBatchImportHistory, long>().Delete(targetBatch);

                if (successfulBatches == 0)
                {
                    var notificationErrorsDto = new NotificationDto(
                       entity: NotificationEntity.Candidate,
                       operation: NotificationOperation.Imported,
                       from: Resource.NotificationFromHangfire,
                       status: NotificationStatus.Failed,
                       type: NotificationTypeStatus.Error,
                       parameterName: $"{string.Join("; ", errors)}: {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorsDto, parsedUserId);
                }
                else
                {
                    var notificationErrorDto = new NotificationDto(
                         entity: NotificationEntity.Candidate,
                         operation: NotificationOperation.Imported,
                         from: Resource.NotificationFromHangfire,
                         status: NotificationStatus.Success,
                         type: NotificationTypeStatus.Success,
                         parameterName: $"{string.Join("; ", errors)}: {successfulBatches} / {totalBatches}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorDto, parsedUserId);
                }
            }
            else
            {
                await SendCandidateImportSuccessNotificationAsync(filterParamsValues, parsedUserId);
            }
        }

        [JobDisplayName("Oes System - Allocate Candidates To Schedule Papers"), AutomaticRetry(Attempts = 0)]
        public async Task ProcessAllocateCandidatesAsync(AllocateLookUpsSchedulePaperRequestDto allocateLookUpsSchedulePaperRequestDto, FilterParamsValues filterParamsValues)
        {
            var parsedUserId = Guid.Parse(filterParamsValues.UserId);

            using (var command = _commonService._unitOfWork.CreateDbCommand())
            {
                command.CommandText = "AllocateCandidatesToSchedulePapers";
                command.CommandType = CommandType.StoredProcedure;

                var lookUpIdsJson = JsonSerializer.Serialize(allocateLookUpsSchedulePaperRequestDto.LookUpIds);

                var inputLookUpIds = new MySqlParameter("@LookUpIds", MySqlDbType.JSON)
                {
                    Value = lookUpIdsJson
                };

                var inputSchedulePaperId = new MySqlParameter("@SchedulePaperId", MySqlDbType.Int32)
                {
                    Value = allocateLookUpsSchedulePaperRequestDto.SchedulePaperId
                };

                var inputVenueId = new MySqlParameter("@VenueId", MySqlDbType.Int32)
                {
                    Value = allocateLookUpsSchedulePaperRequestDto.VenueId
                };

                var inputOrgSignature = new MySqlParameter("@OrgSignature", MySqlDbType.VarChar, 255)
                {
                    Value = filterParamsValues.Signature
                };

                var inputCreationUser = new MySqlParameter("@CreationUser", MySqlDbType.VarChar, 255)
                {
                    Value = filterParamsValues.UserEmail
                };

                var inputOrganizationId = new MySqlParameter("@OrganizationId", MySqlDbType.Int32)
                {
                    Value = filterParamsValues.OrganizationId
                };

                var returnStatusParam = new MySqlParameter("@status", MySqlDbType.Int32)
                {
                    Direction = ParameterDirection.Output
                };

                var MessageParam = new MySqlParameter("@Message", MySqlDbType.VarChar, 255)
                {
                    Direction = ParameterDirection.Output
                };

                command.Parameters.AddRange(new[]
                {
                    inputLookUpIds,
                    inputCreationUser,
                    inputSchedulePaperId,
                    inputVenueId,
                    inputOrgSignature,
                    inputOrganizationId,
                    returnStatusParam,
                    MessageParam
                });

                await _commonService._unitOfWork.OpenConnectionAsync();
                await command.ExecuteNonQueryAsync();

                int returnStatus = Convert.ToInt32(returnStatusParam.Value);
                string outMessage = Convert.ToString(MessageParam.Value);

                if (returnStatus == 0)
                {
                    var notificationErrorDto = new NotificationDto(
                        entity: NotificationEntity.Candidate,
                        operation: NotificationOperation.Imported,
                        from: Resource.NotificationFromHangfire,
                        status: NotificationStatus.Failed,
                        type: NotificationTypeStatus.Error,
                        parameterName: $"{outMessage}"
                    );

                    await _notificationHubService.NotifyAsync(notificationErrorDto, parsedUserId);
                }

                if (returnStatus == 2 || returnStatus == 3 || returnStatus == 4)
                {
                    var notificationeErrorDto = new NotificationDto(
                        entity: NotificationEntity.Candidate,
                        operation: NotificationOperation.Imported,
                        from: Resource.NotificationFromHangfire,
                        status: NotificationStatus.Success,
                        type: NotificationTypeStatus.Success,
                        parameterName: $"{outMessage}"
                    );

                    await _notificationHubService.NotifyAsync(notificationeErrorDto, parsedUserId);
                }

                await SendCandidateAllocateSuccessNotificationAsync(filterParamsValues, parsedUserId);
            }
        }

        #endregion Hangfire Methods
    }
}