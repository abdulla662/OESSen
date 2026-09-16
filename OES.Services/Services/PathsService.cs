using AutoMapper;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using OES.Core.Entities;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class PathsService(IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider,
                              ICommonService _commonService,
                              IMapper _mapper,
                              IAPIMemoryCach aPIMemoryCach) : IPathsService
    {
        public IEnumerable<ApiEndPointDTO> GetControllerPaths()
        {
            var controllerActions = _actionDescriptorCollectionProvider
                .ActionDescriptors
                .Items
                .OfType<ControllerActionDescriptor>();

            return [.. controllerActions.Select(action =>
            {
                var httpMethodAttribute = action.MethodInfo
                    .GetCustomAttributes(inherit: true)
                    .OfType<HttpMethodAttribute>()
                    .FirstOrDefault();

                var routeTemplate = httpMethodAttribute?.Template ?? $"{action.ControllerName}/{action.ActionName}";

                return new ApiEndPointDTO
                {
                    Name = $"api/{action.ControllerName}/{routeTemplate}"
                };
            })];
        }

        public async Task<ApiResponse> AssignRoleToPath(AssignRoleToEndpointDto AssignPatheDTO)
        {
            // Validating that there is no user that's added to the same item twice

            var assignmentValid = await ValidateRoleToPathAssignmentAsync(AssignPatheDTO);

            if (assignmentValid.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return assignmentValid;
            }

            // Adding Roles to the Page

            var PathRoles = new List<ApiEndpointRole>();

            foreach (var item in AssignPatheDTO.RoleIDs)
            {
                PathRoles.Add(new ApiEndpointRole
                {
                    ApiId = AssignPatheDTO.PathID,
                    RoleId = item
                });
            }

            // Get Current Role for Path
            var currentPathRoles = _commonService._unitOfWork.Repository<ApiEndpointRole, long>().GetAll(o => o.ApiId == AssignPatheDTO.PathID).ToList();

            _commonService
                ._unitOfWork
                .Repository<ApiEndpointRole, long>()
                .DeleteRange(currentPathRoles);


            _commonService
                ._unitOfWork
                .Repository<ApiEndpointRole, long>()
                .AddRangAsync(PathRoles);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                await aPIMemoryCach.GetEndPointData(true);

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.Created,
                                    Resource.RoleshasbeenassignedtothePathsuccessfully);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.FailToAssignRolesToThePath);
        }

        public async Task<ApiResponse> GetAll()
        {
            var PathsInDB = await _commonService._unitOfWork.Repository<ApiEndpoint, long>().GetAllAsync();

            if (!PathsInDB.Any())
            {
                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.NotFound,
                                   HttpStatusCode.NotFound,
                                   "Not Found");
            }
            else
            {
                List<BlazPathDTO> pageDTO = [];

                foreach (var page in PathsInDB)
                {
                    pageDTO.Add(new BlazPathDTO { ID = page.Id, Name = page.Name });
                }

                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.Success,
                                   HttpStatusCode.OK,
                                   "the pages is : ", pageDTO);
            }
        }

        public async Task<ApiResponse> GetPathRole(long PathId)
        {
            var Roles = _commonService
                ._unitOfWork
                .Repository<ApiEndpointRole, long>()
                .GetAll(u => u.ApiId == PathId, null, "Role")
                .Select(x => x.Role)
                .ToList();

            var RolesDtos = _mapper.Map<List<RoleDto>>(Roles);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                null,
                                RolesDtos);
        }

        private async Task<ApiResponse> ValidateRoleToPathAssignmentAsync(AssignRoleToEndpointDto AssignRoleToPageDTO)
        {
            if (AssignRoleToPageDTO.RoleIDs == null || AssignRoleToPageDTO.RoleIDs.Count <= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.UserShouldAtleastRelatedWithOneOrganization,
                    HttpStatusCode.BadRequest,
                    Resource.SelectAtleastOneRole
                );
            }

            return _commonService._apiResponse.GetApiResponse();
        }
    }
}
