using OES.Core.Entities.Paper;
using OES.Helper.Dtos.Section;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class SectionService : ISectionService
    {
        private readonly ICommonService _commonService;

        public SectionService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public async Task<ApiResponse> EditSectionAsync(EditSectionDto sectionDto)
        {
            var section = await _commonService._unitOfWork.Repository<StandardSection, long>().GetObjAsync(s => s.Id == sectionDto.Id);

            if (section == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.SectionNotFound
                );
            }

            section.Name = sectionDto.Name;

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SectionNameUpdatedSuccessfully
            );
        }
    }
}
