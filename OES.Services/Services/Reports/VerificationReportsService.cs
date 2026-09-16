using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using OES.Core.Entities;
using OES.Core.Entities.Schedule;
using OES.Helper.Dtos.Results;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Interface.Interfaces;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace OES.Services.Services.Reports
{
    public class VerificationReportsService : IVerificationReportsService
    {
        private const int MaxErrorLength = 250;

        private readonly ICommonService _commonService;
        private readonly IConfiguration _configuration;
        private readonly VenueDatabase _venueDatabase;

        public VerificationReportsService(ICommonService commonService, IConfiguration configuration)
        {
            _commonService = commonService;
            _configuration = configuration;
            _venueDatabase = _configuration.GetSection(VenueDatabase.SectionName).Get<VenueDatabase>() ?? new();
        }

        public async Task<int> GetVenueCountAsync()
        {
            return await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .CountAsync(x => !x.IsDeleted && x.IsActive);
        }

        public async IAsyncEnumerable<CentersAllocationVerificationResultDto> GetCentersAllocationVerificationReportStreamAsync(
            CentersAllocationVerificationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var examDate = request.ExamDate.ToDateTime(TimeOnly.MinValue);

            var venues = await GetActiveVenuesAsync(cancellationToken);

            var centralCounts = await GetCentersAllocationCountsAsync(examDate, cancellationToken);

            var syncedCounts = centralCounts.Where(x => x.IsSynced).ToDictionary(x => x.VenueId, x => x.Count);

            var notSyncedCounts = centralCounts.Where(x => !x.IsSynced).ToDictionary(x => x.VenueId, x => x.Count);

            var channel = Channel.CreateUnbounded<CentersAllocationVerificationResultDto>();

            _ = Task.Run(async () =>
            {
                try
                {
                    var tasks = venues.Select(async venue =>
                    {
                        var result =
                            await BuildCentersAllocationResultAsync(
                                venue,
                                syncedCounts,
                                notSyncedCounts,
                                examDate,
                                cancellationToken);

                        await channel.Writer.WriteAsync(
                            result,
                            cancellationToken);
                    });

                    await Task.WhenAll(tasks);
                }
                finally
                {
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }

        public async IAsyncEnumerable<VenueAttendanceVerificationResultDto> GetVenueAttendanceVerificationReportStreamAsync(
            VenueAttendanceVerificationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var examDate = request.ExamDate.ToDateTime(TimeOnly.MinValue);

            var examDateEnd = examDate.AddDays(1);

            var venues = await GetActiveVenuesAsync(cancellationToken);

            var centralCounts = await _commonService
                ._unitOfWork
                .Repository<CandidateExamDetails, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                    x.CandidateEndedExam &&
                    x.ExamTrialEndDate >= examDate &&
                    x.ExamTrialEndDate < examDateEnd &&
                    x.CandidateExamDate >= examDate &&
                    x.CandidateExamDate < examDateEnd)
                .GroupBy(x => x.VenueCode)
                .Select(x => new VenueAttendanceCount(
                    x.Key!,
                    x.Count()))
                .ToListAsync(cancellationToken);

            var centralCountsMap = centralCounts
                .ToDictionary(
                    x => x.VenueCode,
                    x => x.Count,
                    StringComparer.OrdinalIgnoreCase);

            var channel = Channel.CreateUnbounded<VenueAttendanceVerificationResultDto>();

            _ = Task.Run(async () =>
            {
                try
                {
                    var tasks = venues.Select(async venue =>
                    {
                        var result = await BuildVenueAttendanceResultAsync(venue, centralCountsMap, examDate, cancellationToken);

                        await channel.Writer.WriteAsync(result, cancellationToken);
                    });

                    await Task.WhenAll(tasks);
                }
                finally
                {
                    channel.Writer.Complete();
                }
            }, cancellationToken);

            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }

        private async Task<List<VenueInfo>> GetActiveVenuesAsync(CancellationToken cancellationToken)
        {
            return await _commonService
                ._unitOfWork
                .Repository<Venue, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .Select(x => new VenueInfo(
                    x.Id,
                    x.Code,
                    x.Name,
                    x.DisplayName,
                    x.IPAddress))
                .ToListAsync(cancellationToken);
        }

        private async Task<List<CentersAllocationCount>> GetCentersAllocationCountsAsync(DateTime examDate, CancellationToken cancellationToken)
        {
            var examDateEnd = examDate.AddDays(1);

            return await _commonService
                ._unitOfWork
                .Repository<SchedulePaperCandidate, long>()
                .Query(applySignature: false, applyOrganizationIdFilter: false)
                .AsNoTracking()
                .Where(x =>
                    x.CandidateExamDate >= examDate &&
                    x.CandidateExamDate < examDateEnd)
                .GroupBy(x => new
                {
                    x.VenueId,
                    x.IsSynced
                })
                .Select(x => new CentersAllocationCount(
                    x.Key.VenueId,
                    x.Key.IsSynced,
                    x.Count()))
                .ToListAsync(cancellationToken);
        }

        private async Task<CentersAllocationVerificationResultDto> BuildCentersAllocationResultAsync(
            VenueInfo venue,
            IReadOnlyDictionary<long, int> syncedCounts,
            IReadOnlyDictionary<long, int> notSyncedCounts,
            DateTime examDate,
            CancellationToken cancellationToken)
        {
            int? localCount = null;

            string? errorMessage = null;

            try
            {
                localCount = await QueryCandidatesPaperCountAsync(venue, examDate, cancellationToken);
            }
            catch (Exception ex)
            {
                errorMessage = TrimError(ex.Message);
            }

            var syncedCount = syncedCounts.GetValueOrDefault(venue.Id);
            var notSyncedCount = notSyncedCounts.GetValueOrDefault(venue.Id);
            var totalExpected = syncedCount + notSyncedCount;

            var status = errorMessage is not null
                ? VerificationStatus.Error.ToString()
                : localCount == totalExpected
                    ? VerificationStatus.Match.ToString()
                    : VerificationStatus.Mismatch.ToString();

            return new CentersAllocationVerificationResultDto
            {
                VenueCode = venue.Code,
                VenueName = !string.IsNullOrEmpty(venue.DisplayName) ? venue.DisplayName : venue.Name,
                SyncedCount = syncedCount,
                NotSyncedCount = notSyncedCount,
                TotalExpected = totalExpected,
                LocalCount = localCount,
                Status = status,
                ErrorMessage = errorMessage
            };
        }

        private async Task<VenueAttendanceVerificationResultDto> BuildVenueAttendanceResultAsync(VenueInfo venue, IReadOnlyDictionary<string, int> centralCounts, DateTime examDate, CancellationToken cancellationToken)
        {
            int? venueCount = null;

            string? errorMessage = null;

            try
            {
                venueCount = await QueryCandidateExamDetailsCountAsync(venue, examDate, cancellationToken);
            }
            catch (Exception ex)
            {
                errorMessage = TrimError(ex.Message);
            }

            var centralCount = centralCounts.GetValueOrDefault(venue.Code, 0);

            var status = errorMessage is not null
                ? VerificationStatus.Error.ToString()
                : venueCount == centralCount
                    ? VerificationStatus.Match.ToString()
                    : VerificationStatus.Mismatch.ToString();

            return new VenueAttendanceVerificationResultDto
            {
                VenueCode = venue.Code,
                VenueName = !string.IsNullOrEmpty(venue.DisplayName) ? venue.DisplayName : venue.Name,
                CentralCount = centralCount,
                VenueCount = venueCount,
                Status = status,
                ErrorMessage = errorMessage
            };
        }

        private async Task<int> QueryCandidatesPaperCountAsync(
            VenueInfo venue,
            DateTime examDate,
            CancellationToken cancellationToken)
        {
            const string query = """
               SELECT COUNT(CandidateId)
               FROM candidatespapers
               WHERE DATE(CandidateExamDate) = @examDate
            """;

            return await ExecuteVenueScalarAsync(
                venue,
                $"{venue.Code.ToLowerInvariant()}_localcesdb",
                query,
                examDate,
                cancellationToken);
        }

        private async Task<int> QueryCandidateExamDetailsCountAsync(
            VenueInfo venue,
            DateTime examDate,
            CancellationToken cancellationToken)
        {
            const string query = """
                SELECT COUNT(CandidateId)
                FROM candidateexamdetailsview
                WHERE CandidateEndedExam = 1
                  AND DATE(ExamTrialEndDate) = @examDate
                  AND DATE(CandidateExamDate) = @examDate
             """;

            return await ExecuteVenueScalarAsync(
                venue,
                $"{venue.Code.ToLowerInvariant()}_localcesdb",
                query,
                examDate,
                cancellationToken);
        }

        private async Task<int> ExecuteVenueScalarAsync(
            VenueInfo venue,
            string databaseName,
            string query,
            DateTime examDate,
            CancellationToken cancellationToken)
        {
            try
            {
                return await ExecuteScalarAsync(
                    venue.IPAddress,
                    databaseName,
                    query,
                    examDate,
                    cancellationToken);
            }
            catch (MySqlException ex) when (
                ex.ErrorCode == MySqlErrorCode.UnknownDatabase &&
                venue.Code.Equals(
                    _venueDatabase.MalazFallbackVenueCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteScalarAsync(
                    venue.IPAddress,
                    _venueDatabase.MalazFallbackDatabase,
                    query,
                    examDate,
                    cancellationToken);
            }
        }

        private async Task<int> ExecuteScalarAsync(
            string ipAddress,
            string databaseName,
            string query,
            DateTime examDate,
            CancellationToken cancellationToken)
        {
            var connectionString = new MySqlConnectionStringBuilder
            {
                Server = ipAddress,
                Port = (uint)_venueDatabase.Port,
                Database = databaseName,
                UserID = _venueDatabase.Username,
                Password = _venueDatabase.Password,
                ConnectionTimeout = (uint)_venueDatabase.ConnectionTimeoutSeconds,
                DefaultCommandTimeout = (uint)_venueDatabase.ConnectionTimeoutSeconds
            }.ConnectionString;

            await using var connection = new MySqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();

            command.CommandText = query;

            command.Parameters.AddWithValue("@examDate", examDate.ToString("yyyy-MM-dd"));

            var scalar = await command.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(scalar);
        }

        private static string TrimError(string message)
        {
            return message.Length > MaxErrorLength
                ? message[..MaxErrorLength]
                : message;
        }


        #region Helper Records

        private sealed record VenueInfo(
            long Id,
            string Code,
            string Name,
            string? DisplayName,
            string IPAddress);

        private sealed record CentersAllocationCount(
            long VenueId,
            bool IsSynced,
            int Count);

        private sealed record VenueAttendanceCount(
            string VenueCode,
            int Count);

        #endregion
    }
}