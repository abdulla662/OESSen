using OES.Core.Entities;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class ValidatorItemBankService : IValidatorItemBankService
    {
        private readonly ICommonService _commonService;

        public ValidatorItemBankService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public async Task<ApiResponse> ValidateItemBankToEdit(EditItemBankNodeDto dto)
        {
            if (dto == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound);
            }

            if (!await _commonService._unitOfWork.Repository<ItemBank, long>().IsExistAsync(x => x.Id == dto.Id && !x.IsDeleted))
            {
                return _commonService
                    ._apiResponse.GetApiResponse(CustomCodeStatus.NotFoundItembank,
                                                 HttpStatusCode.NotFound,
                                                 Resource.ItemBankNotFound);
            }

            var parent = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetObjAsync(x => x.Id == dto.ParentId, nameof(ItemBank.Childreen));

            if (parent == null)
            {
                return _commonService
                    ._apiResponse.GetApiResponse(CustomCodeStatus.NotFoundItembank,
                                                 HttpStatusCode.NotFound,
                                                 Resource.ParentNotFound);
            }

            //if (parent.Hours > 0)
            //{
            //    float totalChildrenHours = parent.Childreen.Where(c => c.Id != dto.Id).Sum(c => c.Hours);

            //    if ((totalChildrenHours + dto.Hours) > parent.Hours)
            //    {
            //        return _commonService
            //            ._apiResponse
            //            .GetApiResponse(CustomCodeStatus.ParentHoursmallerthan,
            //                            HttpStatusCode.BadRequest,
            //                            Resource.ParentHoursSmallerThanError);
            //    }
            //}

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK);
        }
    }
}