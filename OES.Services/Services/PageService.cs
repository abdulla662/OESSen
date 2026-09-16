using AutoMapper;
using OES.Core.Entities;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class PageService : IPageService
    {
        public ICommonService _commonService;

        private readonly IMapper _mapper;

        public PageService(ICommonService commonService, IMapper mapper)
        {
            _commonService = commonService;
            _mapper = mapper;
        }

        public async Task<ApiResponse> GetAll()
        {
            var pagesInDatabase = await _commonService._unitOfWork.Repository<Page, long>().GetAllAsync();

            if (!pagesInDatabase.Any())
            {
                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.NotFound,
                                   HttpStatusCode.NotFound,
                                   "Not Found");
            }
            else
            {
                List<BalzPageDTO> pageDTO = [];

                foreach (var page in pagesInDatabase)
                {
                    pageDTO.Add(new BalzPageDTO { ID = page.Id, Name = page.Name });
                }

                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.Success,
                                   HttpStatusCode.OK,
                                   "The pages is: ", pageDTO);
            }
        }

        public async Task<ApiResponse> AssignRoleToPage(AssignRoleToPageDTO AssignpageDTO)
        {
            // Validating that there is no user that's added to the same item twice

            var assignmentValid = await ValidateRoleToPageAssignmentAsync(AssignpageDTO);

            if (assignmentValid.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return assignmentValid;
            }

            // Adding Roles to the Page

            var PageRoles = new List<PageRoles>();

            foreach (var item in AssignpageDTO.RoleIDs)
            {
                PageRoles.Add(new PageRoles
                {
                    PageId = AssignpageDTO.PageID,
                    RoleId = item
                });
            }

            // Get Current Role for Page

            var currentPageRoles = _commonService._unitOfWork.Repository<PageRoles, long>().GetAll(o => o.PageId == AssignpageDTO.PageID).ToList();

            _commonService
                ._unitOfWork
                .Repository<PageRoles, long>()
                .DeleteRange(currentPageRoles);


            _commonService
                ._unitOfWork
                .Repository<PageRoles, long>()
                .AddRangAsync(PageRoles);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.Created,
                                    Resource.RoleshasbeenassignedtothePagesuccessfully);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.failtoassignRolestothePage);
        }

        private async Task<ApiResponse> ValidateRoleToPageAssignmentAsync(AssignRoleToPageDTO AssignRoleToPageDTO)
        {
            if (AssignRoleToPageDTO.RoleIDs == null || AssignRoleToPageDTO.RoleIDs.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.UserShouldAtleastRelatedWithOneOrganization,
                    HttpStatusCode.BadRequest,
                    Resource.SelectAtleastOneRole
                );
            }

            return _commonService._apiResponse.GetApiResponse();
        }

        public async Task<ApiResponse> GetPageRole(long pageId)
        {
            var Roles = _commonService
                ._unitOfWork
                .Repository<PageRoles, long>()
                .GetAll(u => u.PageId == pageId, null, "Role")
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
    }
}
