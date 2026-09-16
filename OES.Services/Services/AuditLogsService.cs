using MySqlConnector;
using OES.Core.Entities;
using OES.Helper.Dtos.AuditLogs;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using OES.Services.Helpers;
using SharedHelper.General;
using System.Data;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class AuditLogsService : IAuditLogsService
    {
        private readonly IApiResponse _apiResponse;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonService _commonService;
        private readonly IClientIpProviderService _clientIpProviderService;

        public AuditLogsService(
            IApiResponse apiResponse,
            IUnitOfWork unitOfWork,
            ICommonService commonService,
            IClientIpProviderService clientIpProviderService)
        {
            _apiResponse = apiResponse;
            _unitOfWork = unitOfWork;
            _commonService = commonService;
            _clientIpProviderService = clientIpProviderService;
        }

        public async Task<ApiResponse> GetAllAsync(PaginationSearchModel searchModel)
        {
            var (logs, totalRecords) = await GetAuditLogsAsync(searchModel);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.AuditLogsFetchedSuccessfully,
                new CustomTableData<GetAuditLogsDto>(logs, totalRecords)
            );
        }

        private async Task<(List<GetAuditLogsDto> Logs, int TotalRecords)> GetAuditLogsAsync(PaginationSearchModel searchModel)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var filterModel = searchModel;
            AuditLogsFilterDto actionFilter = null;

            if (searchModel.FilterObj is JsonElement outerJson)
            {
                var nested = outerJson.Deserialize<PaginationSearchModel>(options);
                if (nested != null)
                {
                    filterModel = nested;
                    if (nested.FilterObj is JsonElement innerJson)
                        actionFilter = innerJson.Deserialize<AuditLogsFilterDto>(options);
                }
            }

            await using var command = _commonService._unitOfWork.CreateDbCommand();
            command.CommandText = "GetAuditLogs";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.AddRange(new[]
            {
                new MySqlParameter($"@p_{nameof(PaginationSearchModel.SearchKey)}", MySqlDbType.Text)
                {
                    Value = string.IsNullOrWhiteSpace(filterModel.SearchKey)
                        ? DBNull.Value : filterModel.SearchKey.Trim()
                },
                new MySqlParameter($"@p_{nameof(AuditLogsFilterDto.PathName)}", MySqlDbType.Text)
                {
                    Value = string.IsNullOrWhiteSpace(actionFilter?.PathName)
                        ? DBNull.Value : actionFilter.PathName.Trim()
                },
                new MySqlParameter($"@p_{nameof(AuditLogsFilterDto.PageName)}", MySqlDbType.Text)
                {
                    Value = string.IsNullOrWhiteSpace(actionFilter?.PageName)
                        ? DBNull.Value : actionFilter.PageName.Trim()
                },
                new MySqlParameter($"@p_{nameof(PaginationSearchModel.FromDate)}", MySqlDbType.DateTime)
                {
                    Value = (object?)filterModel.FromDate ?? DBNull.Value
                },
                new MySqlParameter($"@p_{nameof(PaginationSearchModel.ToDate)}", MySqlDbType.DateTime)
                {
                    Value = (object?)filterModel.ToDate ?? DBNull.Value
                },
                new MySqlParameter($"@p_{nameof(PaginationSearchModel.PageIndex)}", MySqlDbType.Int32)
                {
                    Value = searchModel.PageIndex
                },
                new MySqlParameter($"@p_{nameof(PaginationSearchModel.PageSize)}", MySqlDbType.Int32)
                {
                    Value = searchModel.PageSize > 0 ? searchModel.PageSize : 10
                },
                new MySqlParameter($"@p_{nameof(PaginationSearchModel.PaginationOff)}", MySqlDbType.Bool)
                {
                    Value = searchModel.PaginationOff
                },
            });

            await _commonService._unitOfWork.OpenConnectionAsync();

            var logs = new List<GetAuditLogsDto>();
            int totalRecords = 0;

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                totalRecords = reader.GetInt32("TotalRecords");

            await reader.NextResultAsync();

            while (await reader.ReadAsync())
            {
                logs.Add(new GetAuditLogsDto
                {
                    Id = reader.GetInt64($"{nameof(GetAuditLogsDto.Id)}"),
                    Action = reader.IsDBNull($"{nameof(GetAuditLogsDto.Action)}") ? null : reader.GetString($"{nameof(GetAuditLogsDto.Action)}"),
                    IPAddress = reader.IsDBNull($"{nameof(GetAuditLogsDto.IPAddress)}") ? null : reader.GetString($"{nameof(GetAuditLogsDto.IPAddress)}"),
                    CreationUser = reader.IsDBNull($"{nameof(GetAuditLogsDto.CreationUser)}") ? null : reader.GetString($"{nameof(GetAuditLogsDto.CreationUser)}"),
                    ActionAt = reader.GetDateTime($"{nameof(GetAuditLogsDto.ActionAt)}"),
                    Url = reader.IsDBNull($"{nameof(GetAuditLogsDto.Url)}") ? null : reader.GetString($"{nameof(GetAuditLogsDto.Url)}"),
                    PathName = reader.IsDBNull($"{nameof(GetAuditLogsDto.PathName)}") ? null : reader.GetString($"{nameof(GetAuditLogsDto.PathName)}"),
                    PageName = reader.IsDBNull($"{nameof(GetAuditLogsDto.PageName)}") ? string.Empty : reader.GetString($"{nameof(GetAuditLogsDto.PageName)}"),
                });
            }

            return (logs, totalRecords);
        }

        public async Task<ExcelFileResult> ExportExcelAsync(PaginationSearchModel searchModel)
        {
            searchModel.PaginationOff = true;
            var (logs, _) = await GetAuditLogsAsync(searchModel);

            var columns = new List<ExcelExportHelper.ColumnDefinition<GetAuditLogsDto>>
            {
                new() { Header = Resource.UserEmail, ValueSelector = x => x.CreationUser },
                new() { Header = Resource.Action,    ValueSelector = x => x.Action },
                new() { Header = Resource.PageName,  ValueSelector = x => x.PageName },
                new() { Header = Resource.PathName,  ValueSelector = x => x.PathName },
                new() { Header = Resource.Time,      ValueSelector = x => x.ActionAt.ToString("yyyy-MM-dd HH:mm:ss") },
                new() { Header = Resource.Url,       ValueSelector = x => x.Url },
                new() { Header = Resource.IPAddress, ValueSelector = x => x.IPAddress },
            };

            return new ExcelFileResult(
                Bytes: ExcelExportHelper.GenerateExcelBytes(logs, Resource.AuditLogs, columns),
                FileName: $"AuditLogs_{DateTimeHelper.Now:yyyy-MM-dd}.xlsx",
                ContentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        public async Task<ApiResponse> SaveBatchAsync(List<AuditLogsDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.NoLogsToSave);
            }

            var entities = dtos.ConvertAll(d => new AuditLog
            {
                Action = d.Action,
                Url = d.Url,
                ActionAt = d.ActionAt == default ? DateTimeHelper.Now : d.ActionAt,
                PathName = d.PathName,
                PageName = d.PageName,
                IPAddress = _clientIpProviderService.GetIpAddress() ?? string.Empty
            });

            _unitOfWork.Repository<AuditLog, long>().AddRangAsync(entities);

            await _unitOfWork.Complete();

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.AuditLogRecorded);
        }
    }
}
