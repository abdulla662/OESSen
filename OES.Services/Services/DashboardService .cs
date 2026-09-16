using OES.Helper.Dtos.DashbBoard;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using SharedHelper.RolesNames;
using System.Net;

namespace OES.Services.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApiResponse _apiResponse;
        private readonly FilterParamsValues _filterParamsValues;

        public DashboardService(
            IApiResponse apiResponse,
            IUnitOfWork unitOfWork,
            FilterParamsValues filterParamsValues
        )
        {
            _apiResponse = apiResponse;
            _unitOfWork = unitOfWork;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<ApiResponse> GetDashboardStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var organizationId = _filterParamsValues.OrganizationId;
            var organizationSignature = _filterParamsValues.Signature;
            var userEmail = _filterParamsValues.UserEmail;
            bool isSuperAdminOrEntityAdmin = _filterParamsValues.SsoUserRoles.Any(x => x.Name == AdminRoles.SuperAdmin || x.Name == AdminRoles.Entity_Admin);

            var parameters = new List<(string Name, object Value)>
            {
                ("p_OrganizationId", organizationId),
                ("p_OrganizationSignature", organizationSignature ?? string.Empty),
                ("p_CreationUser", userEmail ?? string.Empty),
                ("p_IsSuperAdminOrEntityAdmin", isSuperAdminOrEntityAdmin ? 1 : 0)
            };

            var result = await _unitOfWork.ExecuteStoredProcedureAsync<DashboardStatisticsDto>(
                "sp_GetDashboardStatistics",
                parameters,
                cancellationToken
            );

            var dto = result.FirstOrDefault() ?? new DashboardStatisticsDto();

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                dto
            );
        }
    }
}