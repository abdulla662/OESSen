using OES.Core.Entities;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using System.Net;

namespace OES.Services.Services
{
    public class AppGroupValidatorService : IAppGroupValidatorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IApiResponse _apiResponse;
        private readonly FilterParamsValues _filterParamsValues;

        public AppGroupValidatorService(IUnitOfWork unitOfWork, IApiResponse apiResponse, FilterParamsValues filterParamsValues)
        {
            _unitOfWork = unitOfWork;
            _apiResponse = apiResponse;
            _filterParamsValues = filterParamsValues;
        }

        public async Task<ApiResponse> ValidateForInsertionAsync(NewGroupDto newGroupDto)
        {
            if (string.IsNullOrWhiteSpace(newGroupDto.GroupName))
            {
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.DuplicateGrouping,
                                    HttpStatusCode.Conflict,
                                    Resource.GroupsNameShouldBeValidValues);
            }

            if (ContainsDuplicates(newGroupDto.RolesId))
            {
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.DuplicateGrouping,
                                    HttpStatusCode.Conflict,
                                    Resource.ThereIsMoreThanOneExistenceOfTheSameRole);
            }

            bool groupExists = await _unitOfWork
                .Repository<OESGroup, long>()
                .IsExistAsync(x => x.Name.ToLower() == newGroupDto.GroupName.ToLower() &&
                              x.OrganizationSignature == _filterParamsValues.Signature);
            if (groupExists)
            {
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.DuplicateGrouping,
                                    HttpStatusCode.Conflict,
                                    Resource.ThereIsAlreadyAGroupExistsWithTheSameName);
            }

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK);
        }

        public async Task<ApiResponse> ValidateForUpdateAsync(GroupUpdateDto groupUpdateDto)
        {
            if (!await _unitOfWork.Repository<OESGroup, Guid>().IsExistAsync(e => e.Id == groupUpdateDto.Id))
            {
                return _apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.NotModified, "Group was not found!", groupUpdateDto);
            }

            if (ContainsDuplicates(groupUpdateDto.RolesId))
            {
                return _apiResponse
                    .GetApiResponse(CustomCodeStatus.DuplicateGrouping,
                                    HttpStatusCode.Conflict,
                                    Resource.ThereIsMoreThanOneExistenceOfTheSameRole);
            }

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK);
        }


        #region Helper Methods
        public bool ContainsDuplicates<T>(IEnumerable<T> enumerable)
        {
            HashSet<T> set = new();

            foreach (var element in enumerable)
            {
                if (!set.Add(element))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
