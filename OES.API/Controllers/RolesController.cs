using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class RolesController : OESBaseController
    {
        private readonly IAppRoleService _appRoleService;


        /// <summary>
        /// Initializes a new instance of the <see cref="AppRolesController"/> class.
        /// </summary>
        /// <param name="appRoleService">The application role service.</param>
        public RolesController(IAppRoleService appRoleService)
        {
            _appRoleService = appRoleService;
        }


        /// <summary>
        /// Retrieves all roles.
        /// </summary>
        /// <returns>Returns a list of all roles.</returns>
        [OESFilter(Authorize = true, ApplyFilter = false)]
        [HttpGet("getAllRoles")]
        public async Task<IApiResponse> GetAllRolesAsync()
        {
            return await _appRoleService.GetRolesAsync();
        }


        /// <summary>
        /// Retrieves roles for a specific page.
        /// </summary>
        /// <param name="pageId">The ID of the page.</param>
        /// <returns>Returns a list of roles for the specified page.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetPageRoles")]
        public async Task<IApiResponse> GetPageRoles(long pageId)
        {
            return await _appRoleService.GetPageRoles(pageId);
        }


        /// <summary>
        /// Retrieves roles for all pages.
        /// </summary>
        /// <returns>Returns a list of roles for all pages.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetAllPagesRoles")]
        public async Task<IApiResponse> GetAllPagesRoles()
        {
            return await _appRoleService.GetAllPagesRoles();
        }
    }
}
