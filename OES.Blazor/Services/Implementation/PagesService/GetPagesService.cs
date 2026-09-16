using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.PagesService;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using System.Reflection;

namespace OES.Blazor.Services.Implementation.PagesService
{
    public class GetPagesService : IGetPagesService
    {
        private readonly IHttpClientHelper _httpClientHelper;

        public GetPagesService(IHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public List<PageDTO> GetPages()
        {
            var Pages = new List<PageDTO>();

            var assembly = Assembly.GetExecutingAssembly();

            var pageComponents = assembly.GetTypes().Where(type => typeof(ComponentBase).IsAssignableFrom(type) && type.GetCustomAttributes(typeof(RouteAttribute), true).Any());

            foreach (var component in pageComponents)
            {
                var routeAttributes = component.GetCustomAttributes<RouteAttribute>();

                foreach (var route in routeAttributes)
                {
                    Pages.Add(new PageDTO { Name = component.Name });
                }
            }

            return Pages;
        }

        public async Task<ApiResponse> sendPagesListToAPI()
        {
            List<PageDTO> PagesList = GetPages();

            return await _httpClientHelper.PostAsync(PagesList, "api/Seeder/SeedPages");
        }
    }
}
