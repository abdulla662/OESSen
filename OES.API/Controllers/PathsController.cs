using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class PathsController(IPathsService _pathsService) : OESBaseController
    {
        [OESFilter(Authorize = true, ApplySignatureFilter = false)]
        [HttpGet("GetAllPathes")]
        public async Task<ApiResponse> GetAllPathes()
        {
            return await _pathsService.GetAll();
        }

        [OESFilter(Authorize = true)]
        [HttpPost("AssignRolesToPath")]
        public async Task<ApiResponse> AssignRolesToPath(AssignRoleToEndpointDto assignRoleToPathDTO)
        {
            return await _pathsService.AssignRoleToPath(assignRoleToPathDTO);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetPathRoles")]
        public async Task<ApiResponse> GetPageRoles(long PathId)
        {
            return await _pathsService.GetPathRole(PathId);
        }
    }
}
