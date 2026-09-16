using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Enums;
using OES.Helper.Interfaces;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Interface.Interfaces;

public class AppRoleService(ICommonService _commonService) : IAppRoleService
{
    public async Task<IApiResponse> GetAllPagesRoles()
    {
        var PageRoles = await _commonService._unitOfWork.Repository<PageRoles, long>().GetAllAsync(x => !x.IsDeleted, null, "Page,Role");

        var PageRolesDTO = PageRoles.Where(p => p.Page is not null && p.Role is not null)
                                    .GroupBy(PageId => PageId.PageId)
                                    .Select(result => new PageRoleDTO
                                    {
                                        PageName = result.First().Page.Name,
                                        Roles = result.Select(roleName => roleName.Role.Name).ToList()
                                    });

        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, System.Net.HttpStatusCode.OK, "Successfully", PageRolesDTO);
    }

    public async Task<IApiResponse> GetPageRoles(long pageId)
    {
        var roles = await _commonService._unitOfWork.Repository<PageRoles, long>().GetAll().Where(page => page.PageId == pageId).Select(page => page.Role.Name).ToListAsync();

        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, System.Net.HttpStatusCode.OK, "successfully", roles);
    }

    public async Task<IApiResponse> GetRolesAsync()
    {
        var query = _commonService._unitOfWork.Repository<OESRole, Guid>().GetAll();

        var data = await query.ToListAsync();

        var mappedData = _commonService._mapper.Map<List<RoleDto>>(data);

        return _commonService._apiResponse.GetApiResponse(
            CustomCodeStatus.Success,
            System.Net.HttpStatusCode.OK,
            null,
            mappedData
        );
    }
}
