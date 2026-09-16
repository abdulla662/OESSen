using OES.Core.Entities;
using OES.Helper.Dtos.ItemBankLevel;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;


namespace OES.Services.Services
{
    public class ItemBankLevelService : IItemBankLevelService
    {

        private readonly ICommonService _commonService;

        public ItemBankLevelService(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public ApiResponse GetAllLevels()
        {
            var ItemBankLevels = _commonService._unitOfWork.Repository<ItemBankLevel, long>().GetAll().ToList();

            List<ItemLevelsDto> levelsDtos = [];

            foreach (var level in ItemBankLevels)
            {
                levelsDtos.Add(new ItemLevelsDto
                {
                    Id = level.Id,
                    Name = level.Name,
                    OrganizationSignature = level.OrganizationSignature,
                });
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, levelsDtos);
        }

        public async Task<ApiResponse> AddLevel(ItemLevelsDto itemBankLevel)
        {
            if (itemBankLevel != null)
            {
                if (await _commonService._unitOfWork.Repository<ItemBankLevel, long>().IsExistAsync(e => e.Name == itemBankLevel.Name))
                {
                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.ItemBankLevelAlreadyExist,
                                                                      HttpStatusCode.BadRequest,
                                                                      Resource.ItemBankLevelAlreadyExist);
                }
                else
                {
                    ItemBankLevel newLevel = new()
                    {
                        Name = itemBankLevel.Name,
                        OrganizationSignature = itemBankLevel?.OrganizationSignature,
                    };

                    await _commonService._unitOfWork.Repository<ItemBankLevel, long>().AddAsync(newLevel);

                    if (await _commonService._unitOfWork.Complete() > 0)
                    {
                        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                          HttpStatusCode.OK,
                                                                          Resource.ItemBankLevelAddedSuccessfully);
                    }
                    else
                    {
                        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                          HttpStatusCode.BadRequest);
                    }
                }
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.ItemBankLevelNotValid,
                                                                  HttpStatusCode.NotFound);
            }
        }
    }
}
