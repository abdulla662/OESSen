using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class SeederController : OESBaseController
    {
        private readonly ISeederService _seederService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeederController"/> class.
        /// </summary>
        /// <param name="seederService">The service responsible for seeding data.</param>
        public SeederController(ISeederService seederService)
        {
            _seederService = seederService;
        }

        /// <summary>
        /// Seeds a list of pages.
        /// </summary>
        /// <param name="ListOfPages">The list of pages to be seeded.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the seeding operation.</returns>
        [OESFilter]
        [HttpPost("SeedPages")]
        public async Task<ApiResponse> SeedPages([FromBody] List<PageDTO> ListOfPages)
        {
            return await _seederService.SeedPages(ListOfPages);
        }

        /// <summary>
        /// Seeds API endpoint roles to protect all endpoints based on role permissions.
        /// </summary>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the seeding operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("SeedApiEndpointRoles")]
        public async Task<ApiResponse> SeedApiEndpointRolesAsync()
        {
            await _seederService.SeedApiEndpointRolesAsync();
            return new ApiResponse { Message = Resource.SeedApiEndpointRolesSuccess };
        }
    }
}
