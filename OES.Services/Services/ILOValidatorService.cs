using OES.Core.Entities;
using OES.Helper.Dtos.ILO;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;

namespace OES.Services.Services
{
    public class ILOValidatorService : IILOValidatorService
    {
        private readonly ICommonService _commonService;
        private readonly FilterParamsValues _filterParamsValues;

        public ILOValidatorService(ICommonService commonService, FilterParamsValues filterParamsValues)
        {
            _commonService = commonService;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<ApiResponse> ValidateNewRoot(ILONewRootDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.Forbidden, System.Net.HttpStatusCode.BadRequest, Resource.PleaseEnterValidFieldData);
            }
            else if (await _commonService._unitOfWork.Repository<ILO, long>().IsExistAsync(x => x.Name == dto.Name))
            {
                return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.ParentOrganizationNotFound, System.Net.HttpStatusCode.BadRequest, Resource.ILONameIsAlreadyExisted);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse();
            }
        }

        public async Task<ApiResponse> ValidateEditRoot(IloEditDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {

                return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.Forbidden, System.Net.HttpStatusCode.BadRequest, Resource.PleaseEnterValidFieldData);
            }
            else if (await _commonService._unitOfWork.Repository<ILO, long>().IsExistAsync(x => x.Name == dto.Name && x.Id != dto.Id && x.OrganizationId == _filterParamsValues.OrganizationId))
            {
                return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.ParentOrganizationNotFound, System.Net.HttpStatusCode.BadRequest, Resource.ILONameIsAlreadyExisted);
            }
            else if (await _commonService._unitOfWork.Repository<ILO, long>().IsExistAsync(x => x.Code == dto.Code && x.Id != dto.Id && x.OrganizationId == _filterParamsValues.OrganizationId))
            {
                return _commonService._apiResponse.GetApiResponse(Helper.Enums.CustomCodeStatus.ParentOrganizationNotFound, System.Net.HttpStatusCode.BadRequest, Resource.ILOCodeIsAlreadyExisted);
            }

            else
            {
                return _commonService._apiResponse.GetApiResponse();
            }
        }
    }
}
