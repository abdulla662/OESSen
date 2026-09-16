using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class PageController : OESBaseController
    {
        private readonly IPageService _pageService;


        /// <summary>
        /// Initializes a new instance of the <see cref="PageController"/> class.
        /// </summary>
        /// <param name="pageService">The service for handling page-related operations.</param>
        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }


        /// <summary>
        /// Retrieves all pages.
        /// </summary>
        /// <returns>An ApiResponse containing a list of all pages.</returns>
        [OESFilter(Authorize = true, ApplySignatureFilter = false)]
        [HttpGet("Getall")]
        public async Task<ApiResponse> GetAllPages()
        {
            return await _pageService.GetAll();
        }


        /// <summary>
        /// Assigns roles to a page.
        /// </summary>
        /// <param name="assignRoleToPageDTO">The details of the roles to be assigned to the page.</param>
        /// <returns>An ApiResponse indicating the result of the role assignment operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AssignRolesToPage")]
        public async Task<ApiResponse> AssignRolesToPage(AssignRoleToPageDTO assignRoleToPageDTO)
        {
            return await _pageService.AssignRoleToPage(assignRoleToPageDTO);
        }


        /// <summary>
        /// Retrieves roles assigned to a page.
        /// </summary>
        /// <param name="PageId">The ID of the page.</param>
        /// <returns>An ApiResponse containing a list of roles assigned to the page.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetPageRoles")]
        public async Task<ApiResponse> GetPageRoles(long PageId)
        {
            return await _pageService.GetPageRole(PageId);
        }
    }
}
