using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.CandidateBatchImportHistory.Requests;
using OES.Helper.Dtos.Document.Response;
using OES.Helper.Dtos.ScheduleCandidate.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class CandidateBatchImportHistoryService : ICandidateBatchImportHistoryService
    {
        private readonly ICommonService _commonService;
        private readonly IFileService _fileService;

        public CandidateBatchImportHistoryService(ICommonService commonService, IFileService fileService)
        {
            _commonService = commonService;
            _fileService = fileService;
        }

        public async Task<ApiResponse> GetAllPaginatedBatchesAsync(PaginationSearchModel paginationSearchModel)
        {
            var api = _commonService._apiResponse;

            if (paginationSearchModel == null)
                return api.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, "Invalid request");

            long? schedulePaperId = null;

            if (paginationSearchModel.FilterObj != null && long.TryParse(paginationSearchModel.FilterObj.ToString(), out var id))
            {
                schedulePaperId = id;
            }

            if (schedulePaperId == null || schedulePaperId <= 0)
                return api.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, "schedule paper id is required");

            var repo = _commonService._unitOfWork.Repository<CandidateBatchImportHistory, long>();

            var query = repo.Query().AsNoTracking().Where(x => x.SchedulePaperId == schedulePaperId);

            if (paginationSearchModel.FromDate.HasValue)
                query = query.Where(x => x.CreationDate >= paginationSearchModel.FromDate.Value);

            if (paginationSearchModel.ToDate.HasValue)
                query = query.Where(x => x.CreationDate <= paginationSearchModel.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
            {
                var key = paginationSearchModel.SearchKey.Trim().ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(key));
            }

            var orderByRaw = (paginationSearchModel.OrderBy ?? string.Empty).Trim().ToUpper();

            bool isDesc = true;

            if (orderByRaw == "ASC" || orderByRaw == "CREATIONDATE ASC")
                isDesc = false;
            else if (orderByRaw == "DESC" || orderByRaw == "CREATIONDATE DESC")
                isDesc = true;

            query = isDesc
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var total = await query.CountAsync();

            if (total == 0)
                return api.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, "No records found");

            if (paginationSearchModel.PaginationOff)
            {
                var all = await query.Select(x => new GetCandidateBatchImportHistoryPaginationDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    FileId = x.FileId,
                    SchedulePaperId = x.SchedulePaperId,
                    CreationUser = x.CreationUser,
                    CreationDate = x.CreationDate,
                    IsReversed = x.IsReversed,
                    DataSource = x.DataSource
                }).ToListAsync();

                return api.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, all);
            }
            else
            {
                var pageIndex = Math.Max(0, paginationSearchModel.PageIndex);

                var pageSize = Math.Max(1, paginationSearchModel.PageSize);

                var page = await query.Skip(pageIndex * pageSize).Take(pageSize).Select(x => new GetCandidateBatchImportHistoryPaginationDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    FileId = x.FileId,
                    SchedulePaperId = x.SchedulePaperId,
                    CreationUser = x.CreationUser,
                    CreationDate = x.CreationDate,
                    IsReversed = x.IsReversed,
                    DataSource = x.DataSource
                }).ToListAsync();

                var table = new CustomTableData<GetCandidateBatchImportHistoryPaginationDto>(page, total);

                return api.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, table);
            }
        }

        public async Task<ApiResponse> GetBatchByIdAsync(long id)
        {
            var entity = await _commonService
                ._unitOfWork
                .Repository<CandidateBatchImportHistory, long>()
                .GetObjAsync(x => x.Id == id);

            if (entity == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.BatchNotFound
                );
            }

            var getCandidateBatchImportHistoryPaginationDto = _commonService._mapper.Map<GetCandidateBatchImportHistoryPaginationDto>(entity);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                getCandidateBatchImportHistoryPaginationDto
            );
        }

        public async Task<ApiResponse> AddBatchAsync(AddCandidateBatchImportHistoryRequestDto addCandidateBatchImportHistoryRequestDto)
        {
            if (addCandidateBatchImportHistoryRequestDto == null ||
                addCandidateBatchImportHistoryRequestDto.File == null ||
                addCandidateBatchImportHistoryRequestDto.SchedulePaperId <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidDataProvided
                );
            }

            var schedulePaper = await _commonService
                ._unitOfWork
                .Repository<SchedulePaper, long>()
                .GetObjAsync(s => s.Id == addCandidateBatchImportHistoryRequestDto.SchedulePaperId);

            if (schedulePaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.SchedulePaperNotFound
                );
            }

            var existingBatches = await _commonService
                ._unitOfWork
                .Repository<CandidateBatchImportHistory, long>()
                .GetAllAsync(s => s.SchedulePaperId == addCandidateBatchImportHistoryRequestDto.SchedulePaperId);

            int incrementalNumber = existingBatches.Count() + 1;

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(addCandidateBatchImportHistoryRequestDto.File.FileName);

            var fileExtension = Path.GetExtension(addCandidateBatchImportHistoryRequestDto.File.FileName);

            var generatedName = $"{fileNameWithoutExtension}_{addCandidateBatchImportHistoryRequestDto.SchedulePaperId}_{incrementalNumber}_{DateTimeHelper.Now.Ticks}{fileExtension}";

            var uploadToDoclibResponse = await _fileService.UploadCandidatesExcelFileAsync(addCandidateBatchImportHistoryRequestDto.File, generatedName);

            if (uploadToDoclibResponse.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    uploadToDoclibResponse.Message
                );
            }

            _ = Guid.TryParse((uploadToDoclibResponse.Data as DocumentMetadataResponseDto).Id.ToString(), out Guid fileId);

            var batchImportHistoryRecord = new CandidateBatchImportHistory
            {
                Name = generatedName,
                FileId = fileId,
                SchedulePaperId = addCandidateBatchImportHistoryRequestDto.SchedulePaperId,
                DataSource = DataSource.Excel
            };

            await _commonService
                ._unitOfWork
                .Repository<CandidateBatchImportHistory, long>()
                .AddAsync(batchImportHistoryRecord);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CandidatesBatchRecordAddedSuccessfully,
                batchImportHistoryRecord
            );
        }

        public async Task<ApiResponse> ReverseBatchAsync(GetCandidateBatchImportHistoryPaginationDto getCandidateBatchImportHistoryPaginationDto)
        {
            if (getCandidateBatchImportHistoryPaginationDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.Importhistorynotfound
                );
            }

            if (getCandidateBatchImportHistoryPaginationDto.IsReversed)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.BatchIsReversed
                );
            }

            var affectedRows = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .Query()
                .Where(s => s.BatchId == getCandidateBatchImportHistoryPaginationDto.Id && s.SchedulePaperId == getCandidateBatchImportHistoryPaginationDto.SchedulePaperId)
                .ExecuteDeleteAsync();

            if (affectedRows == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.EmptyBatch
                );
            }

            var targetBatchRecord = await _commonService
                ._unitOfWork
                .Repository<CandidateBatchImportHistory, long>()
                .GetObjAsync(x => x.Id == getCandidateBatchImportHistoryPaginationDto.Id);

            targetBatchRecord.IsReversed = true;

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.BatchReversedSuccessfully
            );
        }

        public async Task<ApiResponse> DeleteBatchAsync(long id)
        {
            var targetBatchRecord = await _commonService
                ._unitOfWork
                .Repository<CandidateBatchImportHistory, long>()
                .GetObjAsync(s => s.Id == id);

            if (targetBatchRecord == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                   CustomCodeStatus.NotFound,
                   HttpStatusCode.NotFound,
                   Resource.BatchNotFound
                );
            }

            _commonService._unitOfWork.Repository<CandidateBatchImportHistory, long>().SoftDelete(targetBatchRecord);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
               CustomCodeStatus.Success,
               HttpStatusCode.OK,
               Resource.CandidatesBatchRecordDeletedSuccessfully
            );
        }

        public async Task<IActionResult> ExportBatchCandidatesAsync(long batchId)
        {
            var candidates = await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .Query()
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .Include(x => x.Candidate)
                .Include(x => x.Venue)
                .Select(x => new GetBatchCandidatesExportDto
                {
                    CandidateCode = x.Candidate.CandidateCode,
                    Name = x.Candidate.Name,
                    NationalId = x.Candidate.NationalId,
                    UserName = x.Candidate.UserName,
                    Password = x.Candidate.Password,
                    Qualification = x.Candidate.Qualification ?? string.Empty,
                    DateOfBirth = x.Candidate.DateOfBirth,
                    Address = x.Candidate.Address ?? string.Empty,
                    Mobile = x.Candidate.Mobile ?? string.Empty,
                    Email = x.Candidate.Email,
                    Gender = x.Candidate.Gender,
                    RegistrationCenterCode = x.Candidate.RegistrationCenterCode ?? string.Empty,
                    RegistrationDateTime = x.Candidate.RegistrationDateTime,
                    RegistrationNumber = x.RegistrationNumber,
                    VenueCode = x.Venue.Code,
                    CandidateExamDate = x.CandidateExamDate
                })
                .ToListAsync();

            if (candidates.Count == 0)
            {
                return new NotFoundObjectResult(
                    _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.NotFound,
                        HttpStatusCode.NotFound,
                        Resource.BatchNotFound
                    )
                );
            }

            using var workbook = new XLWorkbook();
            GenerateBatchCandidatesSheet(candidates, workbook);

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"CBT_Batch_{batchId}_{DateTime.Now.Ticks}.xlsx";

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }

        private static void GenerateBatchCandidatesSheet(List<GetBatchCandidatesExportDto> candidates, XLWorkbook workbook)
        {
            var sheet = workbook.Worksheets.Add("Candidates");

            sheet.Cell(1, 1).Value = nameof(Candidate.CandidateCode);
            sheet.Cell(1, 2).Value = nameof(Candidate.Name);
            sheet.Cell(1, 3).Value = nameof(Candidate.NationalId);
            sheet.Cell(1, 4).Value = nameof(Candidate.UserName);
            sheet.Cell(1, 5).Value = nameof(Candidate.Password);
            sheet.Cell(1, 6).Value = nameof(Candidate.Qualification);
            sheet.Cell(1, 7).Value = nameof(Candidate.DateOfBirth);
            sheet.Cell(1, 8).Value = nameof(Candidate.Address);
            sheet.Cell(1, 9).Value = nameof(Candidate.Mobile);
            sheet.Cell(1, 10).Value = nameof(Candidate.Email);
            sheet.Cell(1, 11).Value = nameof(Candidate.Gender);
            sheet.Cell(1, 12).Value = nameof(Candidate.RegistrationCenterCode);
            sheet.Cell(1, 13).Value = nameof(Candidate.RegistrationDateTime);
            sheet.Cell(1, 14).Value = nameof(SchedulePaperCandidate.RegistrationNumber);
            sheet.Cell(1, 15).Value = "VenueCode";
            sheet.Cell(1, 16).Value = nameof(SchedulePaperCandidate.CandidateExamDate);

            int row = 2;

            foreach (var c in candidates)
            {
                sheet.Cell(row, 1).Value = c.CandidateCode;
                sheet.Cell(row, 2).Value = c.Name;
                sheet.Cell(row, 3).Value = c.NationalId;
                sheet.Cell(row, 4).Value = c.UserName;
                sheet.Cell(row, 5).Value = c.Password;
                sheet.Cell(row, 6).Value = c.Qualification ?? string.Empty;
                sheet.Cell(row, 7).Value = c.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty;
                sheet.Cell(row, 8).Value = c.Address ?? string.Empty;
                sheet.Cell(row, 9).Value = c.Mobile ?? string.Empty;
                sheet.Cell(row, 10).Value = c.Email ?? string.Empty;
                sheet.Cell(row, 11).Value = c.Gender;
                sheet.Cell(row, 12).Value = c.RegistrationCenterCode ?? string.Empty;
                sheet.Cell(row, 13).Value = c.RegistrationDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                sheet.Cell(row, 14).Value = c.RegistrationNumber;
                sheet.Cell(row, 15).Value = c.VenueCode ?? string.Empty;
                sheet.Cell(row, 16).Value = c.CandidateExamDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                row++;
            }

            sheet.Columns().AdjustToContents();
        }
    }
}
