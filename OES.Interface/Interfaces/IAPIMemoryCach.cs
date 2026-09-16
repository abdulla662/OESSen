using OES.Helper.PagesEndpointsRolesDtos;

namespace OES.Interface.Interfaces
{
    public interface IAPIMemoryCach
    {
        Task<string[]> GetPathRoles(string path, bool updateRequired);
        Task<List<EndPointsRolesDto>> GetEndPointData(bool UpdateRequired = false);
    }
}
