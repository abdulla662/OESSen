using Microsoft.Extensions.Caching.Memory;
using OES.Core.Entities;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;

namespace OES.Services.Services
{
    public class APIMemoryCach : IAPIMemoryCach
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string CachedKey = "_EndPointsRoles";

        public APIMemoryCach(IMemoryCache memoryCache, IUnitOfWork unitOfWork)
        {
            _memoryCache = memoryCache;
            _unitOfWork = unitOfWork;
        }

        public async Task<string[]> GetPathRoles(string path, bool updateRequired = false)
        {
            var ApiRoles = await GetEndPointData(updateRequired);

            return ApiRoles.Find(x => x.Path == path)?.RolesNames.ToArray();
        }

        public async Task<List<EndPointsRolesDto>> GetEndPointData(bool UpdateRequired = false)
        {
            var Data = _memoryCache.Get(CachedKey);

            if (UpdateRequired || Data == null)
            {
                Data = new List<EndPointsRolesDto>();

                var AllData = _unitOfWork.Repository<ApiEndpointRole, long>().GetAll(x => x.IsActive && !x.IsDeleted, null, "ApiEndpoint,Role");

                Data = AllData.GroupBy(id => id.ApiId).Select(x => new EndPointsRolesDto
                {
                    Path = x.First().ApiEndpoint.Name,
                    RolesNames = x.Select(x => x.Role.Name).ToList()
                })
                .ToList();

                _memoryCache.Set(CachedKey, Data);
            }

            return Data as List<EndPointsRolesDto>;
        }
    }
}
