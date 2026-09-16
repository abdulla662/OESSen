using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Views.ItemBanks;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.AIItemBankGenerator.Response;
using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using SharedHelper.RolesNames;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class ItemBankService : IItemBankService
    {
        private readonly ICommonService _commonService;
        private readonly IValidatorItemBankService _validatorItemBankService;
        private readonly FilterParamsValues _filterParamsValues;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IAutoPermissionAssignmentService _AutoPermissionAssignmentService;
        private readonly IItemBankAuthorizationService _itemBankAuthorizationService;


        public ItemBankService(ICommonService commonService, IMapper mapper, IValidatorItemBankService validatorItemBankService, FilterParamsValues filterParamsValues, INotificationService notificationService, IAutoPermissionAssignmentService iAutoPermissionAssignmentService, IItemBankAuthorizationService itemBankAuthorizationService)
        {
            _commonService = commonService;
            _mapper = mapper;
            _validatorItemBankService = validatorItemBankService;
            _filterParamsValues = filterParamsValues;
            _notificationService = notificationService;
            _AutoPermissionAssignmentService = iAutoPermissionAssignmentService;
            _itemBankAuthorizationService = itemBankAuthorizationService;
        }


        public async Task<IApiResponse> GetByIdAsync(long id)
        {
            var itemBank = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetByIdAsync(id);

            if (itemBank != null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.SuccessfulFetching,
                                    new TreeItemResponseDto
                                    {
                                        Id = itemBank.Id,
                                        Text = itemBank.Name,
                                        Code = itemBank.Code,
                                        Description = itemBank.Description,
                                        ParentId = itemBank.ParentId,
                                        Signature = itemBank.ItemBankSignature,
                                        OrganizationId = itemBank.OrganizationId,
                                        Hours = itemBank.Hours,
                                        Unscored = itemBank.Unscored
                                    });
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.NotFound,
                                HttpStatusCode.NotFound,
                                Resource.ItemBankNotFound);
        }


        public async Task<ApiResponse> EditItemBankNode(EditItemBankNodeDto dto)
        {
            var validationResponse = await _validatorItemBankService.ValidateItemBankToEdit(dto);

            if (validationResponse?.CustomCodeStatus != CustomCodeStatus.Success)
            {
                return validationResponse;
            }
            else
            {
                var targetItemBank = await _commonService._unitOfWork.Repository<ItemBank, long>().GetObjAsync(x => x.Id == dto.Id, nameof(ItemBank.Childreen));

                if (targetItemBank.Childreen != null)
                {
                    //float totalChildrenHours = targetItemBank.Childreen.Where(c => !c.IsDeleted).Sum(c => c.Hours);

                    //if (dto.Hours < totalChildrenHours)
                    //{
                    //    return _commonService
                    //        ._apiResponse
                    //        .GetApiResponse(CustomCodeStatus.Failure,
                    //                        HttpStatusCode.BadRequest,
                    //                        Resource.CannotReduceBelowChildrenHours);
                    //}
                }
            }

            var repo = _commonService._unitOfWork.Repository<ItemBank, long>();

            var trimmedName = (dto.Name ?? "").Trim();
            var trimmedCode = (dto.Code ?? "").Trim();

            var normalizedName = trimmedName.ToLower();
            var normalizedCode = trimmedCode.ToLower();

            var existingItemBank = await repo.GetObjAsync(x =>
                x.Id != dto.Id &&
                x.ParentId == dto.ParentId &&
                (
                    (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Trim().ToLower() == normalizedName) ||
                    (!string.IsNullOrWhiteSpace(x.Code) && x.Code.Trim().ToLower() == normalizedCode)
                )
            );

            if (existingItemBank != null)
            {
                var sameName = (existingItemBank.Name ?? "").Trim().ToLower() == normalizedName;
                var sameCode = (existingItemBank.Code ?? "").Trim().ToLower() == normalizedCode;

                string message = string.Empty;

                if (sameName && sameCode)
                    message = Resource.ItemBankNameAndCodeAlreadyExist;
                else if (sameName)
                    message = Resource.ItemBankNameAlreadyExists;
                else if (sameCode)
                    message = Resource.ItemBankCodeAlreadyExists;

                if (!string.IsNullOrEmpty(message))
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.Conflict,
                        message
                    );
                }
            }

            var existingNode = await repo.GetObjAsync(x => x.Id == dto.Id, Including: "ItemBankGroups.OESGroup");

            if (existingNode == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ItemBankNotFound
                );
            }

            var parentNode = await repo.GetObjAsync(x => x.Id == dto.ParentId);

            var groupRepo = _commonService._unitOfWork.Repository<ItemBankGroups, long>();

            var oldGroups = await groupRepo.GetAllAsync(
                x => x.ItemBankId == existingNode.Id,
                Including: "OESGroup"
            );

            var ownerGroup = oldGroups.FirstOrDefault(g => g.OESGroup != null && g.OESGroup.AutoCreatedForUser);

            var requestedGroupIds = dto.OESGroupDtos
                .Select(g => g.Id)
                .ToHashSet();

            var toRemove = oldGroups
                .Where(g =>
                    g.OESGroup != null &&
                    !g.OESGroup.AutoCreatedForUser &&
                    !requestedGroupIds.Contains(g.OESGroupId)
                )
                .ToList();

            groupRepo.DeleteRange(toRemove);

            var existingManualIds = oldGroups
                .Where(g => g.OESGroup != null && !g.OESGroup.AutoCreatedForUser)
                .Select(g => g.OESGroupId)
                .ToHashSet();

            var toAdd = requestedGroupIds.Except(existingManualIds);

            foreach (var gid in toAdd)
            {
                await groupRepo.AddAsync(new ItemBankGroups
                {
                    ItemBankId = existingNode.Id,
                    OESGroupId = gid
                });
            }

            if (ownerGroup == null)
            {
                var parentNodeWithGroups = await repo.GetObjAsync(
                    x => x.Id == dto.ParentId,
                    "ItemBankGroups.OESGroup"
                );

                var parentOwnerGroupId = parentNodeWithGroups?
                    .ItemBankGroups
                    .FirstOrDefault(g => g.OESGroup.AutoCreatedForUser && !g.IsDeleted)
                    ?.OESGroupId;

                if (parentOwnerGroupId != null)
                {
                    await groupRepo.AddAsync(new ItemBankGroups
                    {
                        ItemBankId = existingNode.Id,
                        OESGroupId = parentOwnerGroupId.Value
                    });
                }
            }

            bool unscoredStatusChanged = existingNode.Unscored != dto.Unscored;

            if (dto.IsActive && existingNode.ParentId.HasValue)
            {
                var parent = await repo.GetObjAsync(x => x.Id == existingNode.ParentId.Value);
                if (parent != null && !parent.IsActive)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.BadRequest,
                        Resource.CannotActivateNodeBecauseParentIsInactive
                    );
                }
            }

            bool isActiveChanged = existingNode.IsActive != dto.IsActive;

            existingNode.Name = trimmedName;
            existingNode.Code = trimmedCode;
            existingNode.Description = dto.Description;
            existingNode.Hours = dto.Hours;
            existingNode.ParentId = dto.ParentId;
            existingNode.ItemBankSignature = parentNode?.ItemBankSignature ?? existingNode.ItemBankSignature;
            existingNode.IsActive = dto.IsActive;
            existingNode.Unscored = dto.Unscored;

            if (isActiveChanged)
            {
                var descendants = await GetNestedNodesOfItemBankAsync(existingNode.Id);

                foreach (var desc in descendants)
                    desc.IsActive = dto.IsActive;
            }

            existingNode.ItemBankGroups =
            [
                .. oldGroups.Where(g => g.OESGroup != null && g.OESGroup.AutoCreatedForUser),
                .. dto.OESGroupDtos.Select(x => new ItemBankGroups
                {
                    OESGroupId = x.Id,
                    ItemBankId = dto.Id
                }),
            ];

            if (unscoredStatusChanged)
                await CascadeUnscoredToDescendantsAsync(existingNode.Id, dto.Unscored);

            var groupRepo2 = _commonService._unitOfWork.Repository<ItemBankGroups, long>();
            var existingGroups = existingNode.ItemBankGroups.ToList();
            var ownerGroup2 = existingGroups.FirstOrDefault(g => g.OESGroup != null && g.OESGroup.AutoCreatedForUser);
            var requestedGroupIdsList = dto.OESGroupDtos.ConvertAll(g => g.Id);

            foreach (var group in existingGroups)
            {
                bool isOwnerGroup = ownerGroup2 != null && group.OESGroupId == ownerGroup2.OESGroupId;

                bool isStillSelected = requestedGroupIdsList.Contains(group.OESGroupId);

                if (!isOwnerGroup && !isStillSelected)
                {
                    groupRepo2.Delete(group);
                }
            }

            foreach (var groupId in requestedGroupIdsList)
            {
                bool alreadyExists = existingGroups.Any(g => g.OESGroupId == groupId);

                if (!alreadyExists)
                {
                    await groupRepo2.AddAsync(new ItemBankGroups
                    {
                        ItemBankId = existingNode.Id,
                        OESGroupId = groupId
                    });
                }
            }

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ItemBankNodeUpdatedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailToUpdateThisItemBank
            );
        }


        public async Task<List<TreeItemResponseDto>> GetByParentIdAndSignatureAsync(TreeItemRequestDto itemBankRequestDto)
        {
            var rootItemBanks = (await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetAllAsync(itemBank => itemBank.ParentId == itemBankRequestDto.ParentId && (string.IsNullOrWhiteSpace(itemBankRequestDto.Signature) || itemBank.ItemBankSignature == itemBankRequestDto.Signature)))
                .OrderByDescending(x => x.Id)
                .ToList();

            var itemBanksDtos = new ConcurrentBag<TreeItemResponseDto>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 3
            };

            await Parallel.ForEachAsync(rootItemBanks, parallelOptions, async (itemBank, _) =>
            {
                var dto = new TreeItemResponseDto
                {
                    Id = itemBank.Id,
                    Text = itemBank.Name,
                    Code = itemBank.Code,
                    Description = itemBank.Description,
                    Signature = itemBank.ItemBankSignature,
                    ParentId = itemBank.ParentId,
                    //Hours = itemBank.Hours,
                    IsActive = itemBank.IsActive,
                    OrganizationId = itemBank.OrganizationId,
                    Unscored = itemBank.Unscored
                };

                itemBanksDtos.Add(dto);
            });

            return [.. itemBanksDtos];
        }


        public async Task<IApiResponse> CanSoftDeleteItemBankAsync(long id)
        {
            var linkedPapersCodes = await GetLinkedPapersCodesAsync(id);

            if (linkedPapersCodes == null || linkedPapersCodes.Count == 0)
            {
                return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.ThisItemBankIsEligibleForDeletion);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        string.Format(
                                            Resource.ItemBanksLinkedToPapersWithCodes,
                                            string.Join(
                                                "<br/>",
                                                linkedPapersCodes
                                                    .Distinct()
                                                    .OrderBy(code => code)
                                                    .Select(code => $"- {WebUtility.HtmlEncode(code)}")
                                            )
                                        )
                    );
        }


        private async Task<List<string>> GetLinkedPapersCodesAsync(long itemBankId)
        {
            var nestedItemBanks = await GetNestedNodesOfItemBankAsync(itemBankId);

            var itemBankIdsToCheck = new List<long> { itemBankId };

            itemBankIdsToCheck.AddRange(nestedItemBanks.Select(ib => ib.Id));

            var itemBankPoints = await _commonService._unitOfWork.Repository<PaperItemBankPoint, long>().GetAllAsync(ibp => itemBankIdsToCheck.Contains(ibp.ItemBankId), Including: nameof(PaperItemBankPoint.Paper));

            return [.. itemBankPoints.Select(ibp => ibp.Paper.Code)];
        }


        public async Task<ApiResponse> ExecuteSoftDeleteForNodeAsync(long id)
        {
            var repository = _commonService._unitOfWork.Repository<ItemBank, long>();

            var groupRepo = _commonService._unitOfWork.Repository<ItemBankGroups, long>();

            var itemBankList = new List<ItemBank>
            {
                await repository.GetByIdAsync(id)
            };

            itemBankList.AddRange(await GetNestedNodesOfItemBankAsync(id));

            itemBankList.ForEach(repository.SoftDelete);

            foreach (var node in itemBankList)
            {
                var groups = await groupRepo.GetAllAsync(g => g.ItemBankId == node.Id);

                foreach (var g in groups)
                {
                    g.IsDeleted = true;
                }

                groupRepo.UpdateRange(groups.ToList());
            }

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService
                  ._apiResponse
                  .GetApiResponse(CustomCodeStatus.Success,
                                  HttpStatusCode.OK,
                                  Resource.ItemBankandallitsnodeshavebeendeletedsuccessfully);
            }

            return _commonService
                  ._apiResponse
                  .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                  HttpStatusCode.Conflict,
                                  Resource.FailTodeletecurrentItemBank);
        }


        public async Task<IApiResponse> ExecuteSoftDeleteForRootAsync(long id)
        {
            var repository = _commonService._unitOfWork.Repository<ItemBank, long>();

            var rootItemBank = await repository.GetByIdAsync(id);

            repository.SoftDelete(rootItemBank);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService
                  ._apiResponse
                  .GetApiResponse(CustomCodeStatus.Success,
                                  HttpStatusCode.OK,
                                  Resource.ItemBankDeletedSuccessfully);
            }

            return _commonService
                  ._apiResponse
                  .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                  HttpStatusCode.Conflict,
                                  Resource.FailTodeletecurrentItemBank);
        }


        public async Task<IApiResponse> TransferChildrenForNodeAsync(long id)
        {
            var repository = _commonService._unitOfWork.Repository<ItemBank, long>();

            var nodeItemBank = await repository.GetByIdAsync(id);

            repository.SoftDelete(nodeItemBank);

            var directChildrenList = (await repository.GetAllAsync(item => item.ParentId == id)).ToList();

            directChildrenList.ForEach(itemBank => itemBank.ParentId = nodeItemBank.ParentId);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService
                  ._apiResponse
                  .GetApiResponse(CustomCodeStatus.Success,
                                  HttpStatusCode.OK,
                                  Resource.ItemBankhasbeendeletedanditsdirectnodeshavebeentransferredtoahigherparentnodesuccessfully);
            }

            return _commonService
                  ._apiResponse
                  .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                  HttpStatusCode.Conflict,
                                  Resource.FailToDeleteItemBankAndTransferringItsNodes);
        }


        private async Task<List<ItemBank>> GetNestedNodesOfItemBankAsync(long parentId)
        {
            var itemBankRepository = _commonService._unitOfWork.Repository<ItemBank, long>();

            var targetParentItemBank = await itemBankRepository.GetObjAsync(x => x.Id == parentId);

            // Step 1: Get all potential descendants of the parent item bank, in ONE query
            var allItemBanks = await itemBankRepository
                .Query()
                .Where(x => x.ParentId != null && x.Id != parentId && !x.IsDeleted && x.ItemBankSignature == targetParentItemBank.ItemBankSignature)
                .ToListAsync();

            // Step 2: Build descendant list in memory using iterative approach
            var descendants = new List<ItemBank>();
            var currentLevelIds = new HashSet<long> { parentId };
            var processed = new HashSet<long>();

            while (currentLevelIds.Count > 0)
            {
                var nextLevelIds = new HashSet<long>();

                foreach (var itemBank in allItemBanks)
                {
                    if (itemBank.ParentId.HasValue &&
                        currentLevelIds.Contains(itemBank.ParentId.Value) &&
                        !processed.Contains(itemBank.Id)
                    )
                    {
                        descendants.Add(itemBank);
                        nextLevelIds.Add(itemBank.Id);
                        processed.Add(itemBank.Id);
                    }
                }

                currentLevelIds = nextLevelIds;
            }

            return descendants;
        }


        public async Task<IApiResponse> GetItemBankByIDAsync(long id)
        {
            if (await _commonService._unitOfWork.Repository<ItemBank, long>().IsExistAsync(e => e.Id == id))
            {
                var itemBank = await _commonService._unitOfWork.Repository<ItemBank, long>().GetObjAsync(e => e.Id == id);

                if (itemBank == null)
                {
                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.ItemBankNotFound);
                }

                var ItemBankGroups = await _commonService._unitOfWork.Repository<ItemBankGroups, long>().GetAllAsync(x => x.ItemBankId == id, null, "OESGroup");

                NewItemBankDTO dTO = new();

                dTO.Id = itemBank.Id;
                dTO.Name = itemBank.Name;
                dTO.Code = itemBank.Code;
                dTO.Description = itemBank.Description;
                dTO.Unscored = itemBank.Unscored;
                dTO.IsActive = itemBank.IsActive;
                dTO.OrganizationId = itemBank.OrganizationId;
                //dTO.Hours = itemBank.Hours;
                if (ItemBankGroups?.Any() == true)
                {
                    dTO.OESGroupDtos.AddRange(ItemBankGroups.ToList().ConvertAll(x => new GetOESGroupDto
                    {
                        Id = x.OESGroupId,
                        Name = x.OESGroup.Name
                    }));
                }

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dTO);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.InternalServerError, Resource.GenericFetchItemBankError);
            }
        }


        public async Task<IApiResponse> UpdateItemBankAsync(NewItemBankDTO bankDTO)
        {
            List<string> errors = [];

            var repo = _commonService._unitOfWork.Repository<ItemBank, long>();

            var groupRepo = _commonService._unitOfWork.Repository<ItemBankGroups, long>();

            var trimmedName = bankDTO.Name?.Trim();
            var trimmedCode = bankDTO.Code?.Trim();

            var similarItemBank = await repo.GetObjAsync(
                x => x.OrganizationId == bankDTO.OrganizationId &&
                     x.Id != bankDTO.Id &&
                     (
                         (
                             !string.IsNullOrWhiteSpace(trimmedCode) &&
                             !string.IsNullOrWhiteSpace(x.Code) &&
                             x.Code.Trim().ToLower() == trimmedCode.ToLower()
                         ) ||
                         (
                             bankDTO.ParentId == null &&
                             x.ParentId == null &&
                             !string.IsNullOrWhiteSpace(x.Name) &&
                             x.Name.Trim().ToLower() == trimmedName.ToLower()
                         )
                     ),
                Including: "ItemBankGroups.OESGroup"
            );

            if (similarItemBank != null)
            {
                var similarInName = similarItemBank.Name.Trim().ToLower() == trimmedName.ToLower();
                var similarInCode = similarItemBank.Code.Trim().ToLower() == trimmedCode.ToLower();

                if (similarInName)
                    errors.Add($"{Resource.Name}: {bankDTO.Name}");

                if (similarInCode)
                    errors.Add($"{Resource.Code}: {bankDTO.Code}");

                if (errors.Count > 0)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.Conflict,
                        string.Format(Resource.DuplicatedItemBankFields, string.Join(" , ", errors))
                    );
                }
            }

            var itemBank = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetObjAsync(
                    e => e.Id == bankDTO.Id,
                    Including: "ItemBankGroups.OESGroup"
                );

            if (itemBank == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.ItemBankNotFound);
            }

            var ownerGroup = itemBank.ItemBankGroups.FirstOrDefault(g => g.OESGroup.AutoCreatedForUser);

            var ownerGroupId = ownerGroup?.OESGroupId ?? Guid.Empty;

            var newGroupsIds = bankDTO.OESGroupDtos.Select(x => x.Id).ToHashSet();

            if (newGroupsIds.Count == 0 && ownerGroupId != Guid.Empty)
            {
                var toRemove = itemBank.ItemBankGroups
                    .Where(g => g.OESGroupId != ownerGroupId)
                    .ToList();

                groupRepo.DeleteRange(toRemove);
            }
            else
            {
                var toRemove = itemBank.ItemBankGroups
                    .Where(g => !newGroupsIds.Contains(g.OESGroupId) && g.OESGroupId != ownerGroupId)
                    .ToList();

                foreach (var removed in toRemove)
                {
                    itemBank.ItemBankGroups.Remove(removed);
                }

                var existingIds = itemBank.ItemBankGroups.Select(g => g.OESGroupId).ToHashSet();

                var toAdd = newGroupsIds
                    .Where(id => id != ownerGroupId && !existingIds.Contains(id))
                    .ToList();

                foreach (var gid in toAdd)
                {
                    await groupRepo.AddAsync(new ItemBankGroups
                    {
                        ItemBankId = bankDTO.Id,
                        OESGroupId = gid
                    });
                }
            }

            bool unscoredStatusChanged = itemBank.Unscored != bankDTO.Unscored;

            bool isActiveChanged = itemBank.IsActive != bankDTO.IsActive;

            itemBank.Name = bankDTO.Name?.Trim();
            itemBank.Code = bankDTO.Code?.Trim();
            itemBank.Description = bankDTO.Description;
            //itemBank.Hours = bankDTO.Hours;
            itemBank.IsActive = bankDTO.IsActive;
            itemBank.Unscored = bankDTO.Unscored;

            if (unscoredStatusChanged)
                await CascadeUnscoredToDescendantsAsync(itemBank.Id, bankDTO.Unscored);

            if (isActiveChanged)
            {
                var descendants = await GetNestedNodesOfItemBankAsync(itemBank.Id);
                foreach (var desc in descendants)
                    desc.IsActive = bankDTO.IsActive;
            }

            // NOTE: Here fetched ItemBank is tracked, and any changes happened to one of its properties are persisted in database when SaveChangesAsync() is executed.
            var affectedRows = await _commonService._unitOfWork.Complete();

            if (affectedRows >= 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.ItembankUpdatedSuccessfully);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.NotModified, Resource.FailToUpdateThisItemBank);
        }


        public async Task<ApiResponse> GetItemBankNodeObject(long Id)
        {
            var itemBankRepo = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetObjAsync(x => x.Id == Id && !x.IsDeleted && x.ParentId != null, Including: "ItemBankGroups.OESGroup");

            if (itemBankRepo == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFoundItembank, HttpStatusCode.NotFound);

            var visibleGroups = itemBankRepo.ItemBankGroups
                .Where(g =>
                    !g.IsDeleted &&
                    g.OESGroup != null &&
                    !g.OESGroup.IsDeleted &&
                    !g.OESGroup.IsTemplate &&
                    !g.OESGroup.AutoCreatedForUser
                )
                .Select(g => new GetOESGroupDto
                {
                    Id = g.OESGroupId,
                    Name = g.OESGroup.Name
                })
                .ToList();

            EditItemBankNodeDto editItemBankNodeDto = new EditItemBankNodeDto()
            {
                Description = itemBankRepo.Description,
                Code = itemBankRepo.Code,
                //Hours = itemBankRepo.Hours,
                Id = Id,
                Name = itemBankRepo.Name,
                ParentId = (long)itemBankRepo.ParentId,
                Unscored = itemBankRepo.Unscored,
                IsActive = itemBankRepo.IsActive,
                ParentIsActive = itemBankRepo.ParentItemBank?.IsActive ?? true,
                OESGroupDtos = visibleGroups
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, editItemBankNodeDto);
        }


        public async Task<ApiResponse> GetParentsForEditNodeObject(long Id)
        {
            var baseItem = await _commonService._unitOfWork.Repository<ItemBank, long>().GetObjAsync(x => x.Id == Id, "Childreen");

            if (baseItem == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFoundItembank, System.Net.HttpStatusCode.NotFound);

            var parents = _commonService._unitOfWork.Repository<ItemBank, long>().GetAll(x => x.Id != Id && x.ParentId != Id, null, "Childreen");

            List<ItemParentList> itemParentLists = [];

            foreach (var parent in parents)
            {
                if (!IsDescendant(baseItem, parent))
                    itemParentLists.Add(new ItemParentList() { Id = parent.Id, Name = parent.Name, ParentId = parent.ParentId });
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, itemParentLists);
        }


        public async Task<IApiResponse> AddItemBank(NewItemBankDTO itemBankDto)
        {
            var repo = _commonService._unitOfWork.Repository<ItemBank, long>();

            var trimmedName = (itemBankDto.Name ?? "").Trim();
            var trimmedCode = (itemBankDto.Code ?? "").Trim();

            var similarItemBank = await repo.GetObjAsync(x =>
                x.OrganizationId == itemBankDto.OrganizationId &&
                (
                    (
                        !string.IsNullOrWhiteSpace(trimmedCode) &&
                        !string.IsNullOrWhiteSpace(x.Code) &&
                        x.Code.Trim().ToLower() == trimmedCode.ToLower()
                    ) ||
                    (
                        itemBankDto.ParentId == null &&
                        x.ParentId == null &&
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.Name.Trim().ToLower() == trimmedName.ToLower()
                    )
                )
            );

            if (similarItemBank != null)
            {
                var similarInName = (similarItemBank.Name ?? "").Trim().ToLower() == trimmedName.ToLower();
                var similarInCode = (similarItemBank.Code ?? "").Trim().ToLower() == trimmedCode.ToLower();

                string message = string.Empty;

                if (similarInName && similarInCode)
                {
                    message = Resource.ItemBankNameAndCodeAlreadyExist;
                }
                else if (similarInName)
                {
                    message = Resource.ItemBankNameAlreadyExists;
                }
                else if (similarInCode)
                {
                    message = Resource.ItemBankCodeAlreadyExists;
                }

                if (!string.IsNullOrEmpty(message))
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Conflict,
                        HttpStatusCode.Conflict,
                        message
                    );
                }
            }

            var itemBank = _mapper.Map<ItemBank>(itemBankDto);

            itemBank.Name = trimmedName;
            itemBank.Code = trimmedCode;
            itemBank.ItemBankSignature = RandomGenerator.GenerateItemBankSignatureCode(itemBank.Name);
            itemBank.ItemBankGroups = [];
            itemBank.Unscored = itemBankDto.Unscored;

            if (itemBankDto.LevelId == 0 && !string.IsNullOrWhiteSpace(itemBankDto.LevelName))
            {
                itemBank.Levels = new ItemBankLevel
                {
                    Name = itemBankDto.LevelName,
                    OrganizationSignature = itemBankDto.OrganizationSignature
                };
            }

            await repo.AddAsync(itemBank);

            var result = await _commonService._unitOfWork.Complete();

            var groupRepo = _commonService._unitOfWork.Repository<ItemBankGroups, long>();

            if (itemBankDto.OESGroupDtos != null && itemBankDto.OESGroupDtos.Count > 0)
            {
                foreach (var group in itemBankDto.OESGroupDtos)
                {
                    await groupRepo.AddAsync(new ItemBankGroups
                    {
                        ItemBankId = itemBank.Id,
                        OESGroupId = group.Id
                    });
                }

                await _commonService._unitOfWork.Complete();
            }

            var isSuperAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin);

            if (!isSuperAdmin)
            {
                await _AutoPermissionAssignmentService
                    .AssignDefaultPermissionsForNewEntityAsync(new AutoPermissionAssignmentRequest
                    {
                        EntityId = itemBank.Id,
                        EntityName = $"Owner Group For - {itemBank.Name}",
                        ResourceType = ResourceType.ItemBank,
                        EntityGroupType = typeof(ItemBankGroups),
                        AdditionalGroupIds = itemBankDto.OESGroupDtos?
                            .Select(g => g.Id)
                            .ToList()
                    });
            }

            if (result > 0)
            {
                if (itemBankDto.OESGroupDtos.Count != 0)
                {
                    var newNotification = new CreateNewNotificationDto
                    {
                        Entity = NotificationEntity.ItemBank,
                        Operation = NotificationOperation.Added,
                        Status = NotificationStatus.Success,
                        AffectedRows = 1,
                        ParameterName = itemBankDto.Name,
                        Type = NotificationTypeStatus.Success,
                        From = Resource.Notification_From_System
                    };

                    var groupIds = itemBankDto.OESGroupDtos.ConvertAll(x => x.Id);

                    await _notificationService.SendNotificationForNewItemBank(groupIds, newNotification);

                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        Resource.ItemBankaddedwithitsgroupssuccessfully
                    );
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.ItemBankAddedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.NotFound,
                Resource.FailToAddItemBank
            );
        }


        public async Task<ApiResponse> GetAllItemBankAsync(PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetAll(e => e.ParentId == null || e.ParentId == 0,
                    Including:
                        $"{nameof(ItemBank.QuestionMetadata)}," +
                        $"{nameof(ItemBank.Childreen)}.{nameof(ItemBank.QuestionMetadata)}"
                ).AsNoTracking();

            var isAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            if (!isAdmin)
            {
                var userGroupIds = _filterParamsValues
                    .OesUserGroupsAndRoles
                    .Select(g => g.GroupId)
                    .ToHashSet();

                query = query.Where(item =>
                    item.ItemBankGroups.Any(g => !g.IsDeleted) &&
                    item.ItemBankGroups.Any(g =>
                        userGroupIds.Contains(g.OESGroupId) &&
                        !g.IsDeleted)
                );
            }

            if (pagination.FromDate.HasValue)
            {
                query = query.Where(x => x.CreationDate >= pagination.FromDate.Value);
            }

            if (pagination.ToDate.HasValue)
            {
                query = query.Where(x => x.CreationDate <= pagination.ToDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey))
                {
                    if (pagination.SearchInName && pagination.SearchInDescription)
                    {
                        query = query.Where(x => x.Name.Contains(pagination.SearchKey) || x.Description.Contains(pagination.SearchKey));
                    }

                    var searchKey = pagination.SearchKey.Trim().ToLower();

                    if (pagination.SearchInName)
                    {
                        query = query.Where(x => !string.IsNullOrWhiteSpace(x.Code) && x.Code.ToLower().Contains(searchKey));
                    }
                    else if (pagination.SearchInDescription)
                    {
                        query = query.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name.ToLower().Contains(searchKey));
                    }
                }

                query = pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

                var totalItems = await query.CountAsync();

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize).ToListAsync();

                var mappedData = await MapItemBanksWithQuestionCountAsync(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    new CustomTableData<NewItemBankDTO>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await (pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate).ToListAsync()
                    : query.OrderBy(x => x.CreationDate).ToListAsync());

                var mappedData = await MapItemBanksWithQuestionCountAsync(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    new CustomTableData<NewItemBankDTO>(mappedData, data.Count)
                );
            }
        }


        public async Task<IApiResponse> AddNewNode(ItemBankNode newItemBankNode)
        {
            var isAdmin = _filterParamsValues.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin);

            newItemBankNode.Name = newItemBankNode.Name?.Trim();

            var CheckName = await _commonService._unitOfWork.Repository<ItemBank, long>().IsExistAsync(x => x.Name.Trim().ToLower() == newItemBankNode.Name.Trim().ToLower() && x.ParentId == newItemBankNode.ParentId);

            if (CheckName)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.Conflict, Resource.SubItemBankAlreadyExists);
            }

            if (!string.IsNullOrWhiteSpace(newItemBankNode.Code))
            {
                var codeExists = await _commonService
                    ._unitOfWork
                    .Repository<ItemBank, long>()
                    .Query()
                    .AnyAsync(x =>
                        x.Code.Trim().ToLower() == newItemBankNode.Code.Trim().ToLower()
                    );

                if (codeExists)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.Conflict,
                        Resource.CodeUsedBefore
                    );
                }
            }

            var parentNode = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query()
                .Where(x => x.Id == newItemBankNode.ParentId && !x.IsDeleted)
                .Include("Childreen")
                .Include("ItemBankGroups.OESGroup")
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (parentNode == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ItemBankParentNotValid,
                    HttpStatusCode.NotFound
                );
            }

            if (!parentNode.IsActive)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.CannotAddNodeBecauseParentIsInactive
                );
            }

            //if (parentNode.Hours > 0 && newItemBankNode.Hours <= 0)
            //{
            //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, Resource.ChildNodeHoursGreaterThanZero);
            //}

            var childrenList = parentNode.Childreen.Where(x => !x.IsDeleted).ToList();

            var isFirstNodeInLevel = IsFirstNodeInLevel(childrenList);

            //var totalStudyHours = childrenList.Sum(x => x.Hours) + newItemBankNode.Hours;

            if (isFirstNodeInLevel)
            {
                if (newItemBankNode.LevelId == 0)
                {
                    var newLevel = new ItemBankLevel
                    {
                        Name = newItemBankNode.Level,
                        OrganizationSignature = parentNode.OrganizationSignature,
                    };

                    await _commonService._unitOfWork.Repository<ItemBankLevel, long>().AddAsync(newLevel);

                    await _commonService._unitOfWork.Complete();

                    newItemBankNode.LevelId = newLevel.Id;
                }
            }
            else
            {
                newItemBankNode.LevelId = (long)childrenList[0].LevelId;
            }

            var userName = _filterParamsValues.UserEmail ?? "System";

            var newNode = new ItemBank
            {
                Name = newItemBankNode.Name,
                Code = newItemBankNode.Code,
                Description = newItemBankNode.Description,
                //Hours = newItemBankNode.Hours,
                LevelId = newItemBankNode.LevelId,
                ParentId = newItemBankNode.ParentId,
                ItemBankSignature = parentNode.ItemBankSignature,
                OrganizationSignature = parentNode.OrganizationSignature,
                OrganizationId = parentNode.OrganizationId,
                Unscored = newItemBankNode.Unscored,
                IsActive = newItemBankNode.IsActive,
                ItemBankGroups = newItemBankNode.OESGroupDtos.ConvertAll(x => new ItemBankGroups
                {
                    OESGroupId = x.Id
                })
            };

            var parentOwnerGroupId = parentNode.ItemBankGroups
                ?.FirstOrDefault(g => g.OESGroup.AutoCreatedForUser && !g.IsDeleted)
                ?.OESGroupId;

            if (parentOwnerGroupId != null && !newNode.ItemBankGroups.Any(g => g.OESGroupId == parentOwnerGroupId))
            {
                newNode.ItemBankGroups.Add(new ItemBankGroups
                {
                    OESGroupId = parentOwnerGroupId.Value
                });
            }

            var similarNodeExists = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .IsExistAsync(parentNode =>
                    parentNode.Id == newNode.ParentId &&
                    parentNode.Name.Trim().ToLower() == newNode.Name.Trim().ToLower() &&
                    !parentNode.IsDeleted
                );

            if (similarNodeExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.Conflict,
                    Resource.SubItemBankAlreadyExists
                );
            }

            await _commonService._unitOfWork.Repository<ItemBank, long>().AddAsync(newNode);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                CreateNewNotificationDto newNotification = new()
                {
                    Entity = NotificationEntity.ItemBank,
                    Operation = NotificationOperation.Added,
                    Status = NotificationStatus.Success,
                    AffectedRows = 1,
                    ParameterName = newNode.Name,
                    Type = NotificationTypeStatus.Success,
                    From = Resource.Notification_From_System
                };

                //var repo = _commonService._unitOfWork.Repository<ItemBank, long>();

                //await repo.AddAsync(newNode);

                //await _commonService._unitOfWork.Complete();

                var groupRepo = _commonService._unitOfWork.Repository<ItemBankGroups, long>();

                parentOwnerGroupId = parentNode.ItemBankGroups
                   ?.FirstOrDefault(g => g.OESGroup.AutoCreatedForUser && !g.IsDeleted)
                   ?.OESGroupId;

                var parentVisibleGroupIds = parentNode.ItemBankGroups
                    .Where(g => !g.OESGroup.AutoCreatedForUser && !g.IsDeleted)
                    .Select(g => g.OESGroupId)
                    .ToList();

                var uiGroupIds = newItemBankNode.OESGroupDtos?
                    .Select(g => g.Id)
                    .ToList() ?? [];

                var finalVisibleGroups = parentVisibleGroupIds
                    .Union(uiGroupIds)
                    .Distinct()
                    .ToList();

                var finalGroupsToSave = new List<Guid>();

                if (parentOwnerGroupId != null)
                    finalGroupsToSave.Add(parentOwnerGroupId.Value);

                finalGroupsToSave.AddRange(finalVisibleGroups);

                foreach (var gid in finalGroupsToSave.Distinct())
                {
                    await groupRepo.AddAsync(new ItemBankGroups
                    {
                        ItemBankId = newNode.Id,
                        OESGroupId = gid,
                        CreationUser = userName
                    });
                }

                await _commonService._unitOfWork.Complete();

                if (parentOwnerGroupId != null)
                {
                    var resourceRepo = _commonService._unitOfWork.Repository<OESResource, long>();
                    var groupResourceRepo = _commonService._unitOfWork.Repository<OESGroupResource, long>();
                    var groupResourceRoleRepo = _commonService._unitOfWork.Repository<OESGroupResourceRole, long>();

                    var itemBankResource = await resourceRepo.GetObjAsync(x => x.Name == ResourceType.ItemBank.ToString() && !x.IsDeleted);

                    var parentGroupResources = await groupResourceRepo.GetAllAsync(r => r.GroupId == parentOwnerGroupId && !r.IsDeleted);

                    foreach (var parentResource in parentGroupResources)
                    {
                        var newGroupResource = new OESGroupResource
                        {
                            GroupId = parentOwnerGroupId.Value,
                            ResourceId = itemBankResource.Id,
                            ResourceType = ResourceType.ItemBank,
                            CreationUser = userName
                        };

                        await groupResourceRepo.AddAsync(newGroupResource);
                        await _commonService._unitOfWork.Complete();

                        var parentRoles = await groupResourceRoleRepo.GetAllAsync(rr => rr.GroupResourceId == parentResource.Id && !rr.IsDeleted);

                        foreach (var r in parentRoles)
                        {
                            await groupResourceRoleRepo.AddAsync(new OESGroupResourceRole
                            {
                                GroupResourceId = newGroupResource.Id,
                                RoleId = r.RoleId,
                                CreationUser = userName
                            });
                        }

                        await _commonService._unitOfWork.Complete();
                    }

                    var addedItemBank = new NewItemBankDTO
                    {
                        Id = newNode.Id,
                        Name = newNode.Name,
                        Code = newNode.Code,
                        Description = newNode.Description,
                        //Hours = newNode.Hours,
                        LevelId = newItemBankNode.LevelId,
                        ParentId = newItemBankNode.ParentId,
                        ItemBankSignature = newNode.ItemBankSignature,
                        OrganizationSignature = newNode.OrganizationSignature,
                        OrganizationId = newNode.OrganizationId,
                        IsActive = newNode.IsActive,
                        OESGroupDtos = newItemBankNode.OESGroupDtos
                    };

                    var groupIds = newItemBankNode.OESGroupDtos.ConvertAll(x => x.Id);

                    await _notificationService.SendNotificationForNewItemBank(groupIds, newNotification);

                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.ItemBankAddedSuccessfully, addedItemBank);
                }
                else
                {
                    if (isAdmin)
                    {
                        var addedItemBank = new NewItemBankDTO
                        {
                            Id = newNode.Id,
                            Name = newNode.Name,
                            Code = newNode.Code,
                            Description = newNode.Description,
                            //Hours = newNode.Hours,
                            LevelId = newItemBankNode.LevelId,
                            ParentId = newItemBankNode.ParentId,
                            ItemBankSignature = newNode.ItemBankSignature,
                            OrganizationSignature = newNode.OrganizationSignature,
                            OrganizationId = newNode.OrganizationId,
                            IsActive = newNode.IsActive,
                            OESGroupDtos = newItemBankNode.OESGroupDtos
                        };

                        var groupIds = newItemBankNode.OESGroupDtos.ConvertAll(x => x.Id) ?? [];

                        await _notificationService.SendNotificationForNewItemBank(groupIds, newNotification);

                        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.ItemBankAddedSuccessfully, addedItemBank);
                    }

                    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong, HttpStatusCode.BadRequest);
                }
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.ItemBankParentNotValid, HttpStatusCode.NotFound);
            }
        }


        public async Task<ApiResponse> AddItemBankTreeAsync(List<AIItemBankNodeDto> trees)
        {
            if (trees == null || trees.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.FailedToAddItemBankPoints,
                    null
                );
            }

            // Check if this tree's root already exists in the database or not, if yes then return error message
            var isRootExist = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .IsExistAsync(x => x.Name.Trim().ToLower() == trees[0].Name.Trim().ToLower() && (x.ParentId == null || x.ParentId == 0));

            if (isRootExist)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.Conflict,
                    Resource.ItemBankRootAlreadyExists,
                    null
                );
            }

            var levelIdCache = new Dictionary<string, long>();

            // Roots are one sibling group (siblings of each other (if any), no parent; but this must not apply as a business rule) — they share one Level too, same rule as any other sibling group.
            var rootLevelId = await ResolveSharedLevelIdAsync(trees, organizationSignature: null, levelIdCache);

            foreach (var root in trees)
            {
                var rootEntity = BuildEntity(root, parentEntity: null, levelId: rootLevelId);

                await _commonService._unitOfWork.Repository<ItemBank, long>().AddAsync(rootEntity);

                await AttachChildrenRecursivelyAsync(root, rootEntity, levelIdCache);
            }

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SavedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.SomethingWentWrong,
                HttpStatusCode.InternalServerError,
                Resource.FailToAddItemBank
            );
        }


        private bool IsFirstNodeInLevel(IEnumerable<ItemBank> itemBanks)
        {
            return !itemBanks.Any();
        }


        private bool CanAddNodeWithStudyHours(IEnumerable<ItemBank> itemBanks, ItemBank parentNode, float newNodeHours)
        {
            if (parentNode.Hours <= 0)
            {
                return true;
            }

            var totalStudyHours = itemBanks.Sum(itemBank => itemBank.Hours) + newNodeHours;

            return totalStudyHours <= parentNode.Hours;
        }


        public async Task<IApiResponse> GetNodeValidation(long ParentId)
        {
            var parentNode = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetObjAsync(x => x.Id == ParentId, "Childreen,Levels");

            var Childs = parentNode.Childreen.ToList();

            var childlevel = Childs.FirstOrDefault();

            if (Childs.Count != 0)
            {
                childlevel.Levels = await _commonService._unitOfWork.Repository<ItemBankLevel, long>().GetByIdAsync((long)childlevel.LevelId);
            }

            ItemBankLevelValidation ItemBankNode = new()
            {
                IsFirstChild = IsFirstNodeInLevel(Childs),
                ChildHours = Childs.Sum(it => it.Hours),
                ParentHours = parentNode.Hours,
                level = childlevel?.Levels?.Name ?? ""
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.ChildList, ItemBankNode);
        }


        public async Task<IApiResponse> GetRootNodeItemBank()
        {
            if (_filterParamsValues.SsoUserRoles.Any(x => x.Name == AdminRoles.SuperAdmin) ||
                _filterParamsValues.SsoUserRoles.Any(x => x.Name == AdminRoles.Entity_Admin)
            )
            {
                var allRoots = await _commonService
                    ._unitOfWork
                    .Repository<ItemBank, long>()
                    .Query()
                    .Where(x => x.ParentId == null || x.ParentId == 0)
                    .OrderByDescending(x => x.CreationDate)
                    .AsNoTracking()
                    .ToListAsync();

                var mappedAll = _mapper.Map<List<RootItemBankDto>>(allRoots);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    mappedAll
                );
            }

            var userGroupIds = _filterParamsValues
                .OesUserGroupsAndRoles
                .Select(g => g.GroupId)
                .Distinct()
                .ToList();

            var rootNodes = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query()
                .Where(x =>
                    (x.ParentId == null || x.ParentId == 0) &&
                    x.ItemBankGroups.Any(ibg =>
                        !ibg.IsDeleted &&
                        (
                            userGroupIds.Contains(ibg.OESGroupId) ||
                            ibg.OESGroup.AutoCreatedForUser &&
                            userGroupIds.Contains(ibg.OESGroupId)
                        )
                    )
                )
                .OrderByDescending(x => x.CreationDate)
                .AsNoTracking()
                .ToListAsync();

            var mappedRootNode = _mapper.Map<List<RootItemBankDto>>(rootNodes);

            return _commonService
                ._apiResponse
                .GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    mappedRootNode
                );
        }


        public async Task<ApiResponse> GetItemBankGroupsAsync(long itemBankId)
        {
            var itemBankGroups = await _commonService
                ._unitOfWork
                .Repository<ItemBankGroups, long>()
                .GetAllAsync(
                    x => x.ItemBankId == itemBankId &&
                         !x.OESGroup.IsTemplate,
                    Including: nameof(ItemBankGroups.OESGroup)
                );

            var itemBankGroupsDto = new ItemBankGroupsDto();

            itemBankGroups.ToList().ForEach(ibg =>
                itemBankGroupsDto.GroupsIds.Add(ibg.OESGroupId));

            itemBankGroupsDto.OwnerGroupId = itemBankGroups
                .FirstOrDefault(x =>
                    x.OESGroup != null &&
                    x.OESGroup.AutoCreatedForUser
                )?.OESGroupId;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                itemBankGroupsDto
            );
        }


        public async Task<ApiResponse> GetUserItemBankGroupsAsync()
        {
            var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";

            var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

            var groups = await groupRepo.GetAllAsync(g =>
                !g.IsDeleted &&
                !g.IsTemplate &&
                !g.IsPredefined &&
                !g.AutoCreatedForUser &&
                g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.ItemBank) &&
                (
                    g.CreationUser.ToLower() == currentUser ||
                    g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
                ),
                Including: nameof(OESGroup.GroupResources)
            );

            var groupDtos = groups
                .Select(g => new GetOESGroupDto
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .DistinctBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                groupDtos
            );
        }


        public async Task<ApiResponse> GetAllItemBanksListAsync()
        {
            var rootItemBanks = await _commonService
              ._unitOfWork
              .Repository<ItemBank, long>()
              .GetAll(x => (x.ParentId == null || x.ParentId == 0) &&
                           (x.QuestionMetadata.Any() || x.Childreen.Any(c => c.QuestionMetadata.Any())))
              .OrderByDescending(x => x.CreationDate)
              .Select(x => new RootItemBankDto
              {
                  Id = x.Id,
                  Name = x.Name,
                  ItemBankSignature = x.ItemBankSignature,
                  OrganizationSignature = x.OrganizationSignature
              })
              .AsNoTracking()
              .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              Resource.SuccessfulFetching,
                                                              rootItemBanks.ToList());
        }


        public async Task<ApiResponse> GetItemBankStatisticsAsync(long itemBankId)
        {
            var metaData = (await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(x => x.ItemBankId == itemBankId &&
                                  x.IsRoot &&
                                  x.ParentId == null,
                             Including: "QuestionType,DifficultyLevel"))
                .ToList();

            var questionTypesStatistics = metaData
                .Where(x => x.QuestionType != null)
                .GroupBy(x => x.QuestionType.Name)
                .Select(group => new ItemBankStatisticsQuestionTypeDto
                {
                    QuestionType = group.Key,
                    QuestionCount = group.Count()
                })
                .ToList();

            var difficultyLevelStatistics = metaData
                .Where(x => x.DifficultyLevel != null)
                .GroupBy(x => x.DifficultyLevel.Name)
                .Select(group => new ItemBankStatisticsDifficultyLevelDto
                {
                    QuestionDifficultyLevel = group.Key,
                    QuestionCount = group.Count()
                })
                .ToList();

            var itemBankStatistics = new ItemBankStatisticsDto
            {
                QuestionsCount = metaData.Count,
                ItemBankStatisticsType = questionTypesStatistics,
                ItemBankStatisticsDifficultyLevel = difficultyLevelStatistics
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                itemBankStatistics
            );
        }


        public async Task<ApiResponse> GetAllItemBankForUserAsync(long itemBankId)
        {
            List<UserItemBankDto> userItemBanksList = [];

            var userRolesNames = _filterParamsValues
                .SsoUserRoles
                .ConvertAll(role => role.Name)
;
            var bypassedSystemRoles = new[] { AdminRoles.Admin, AdminRoles.Entity_Admin, AdminRoles.SuperAdmin };

            if (userRolesNames.Exists(role => bypassedSystemRoles.Contains(role)))
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.SuccessfulFetching,
                                                                  userItemBanksList);
            }

            var rootItemBank = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetObjAsync(x => x.Id == itemBankId);

            if (rootItemBank == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFoundItembank,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.RootItemBankNotFound);
            }

            var userGroupsIds = _filterParamsValues
                .OesUserGroupsAndRoles
                .ConvertAll(x => x.GroupId);

            var itemBanks = await _commonService
                ._unitOfWork
                .Repository<ItemBankGroups, long>()
                .GetAllAsync(x => userGroupsIds.Contains(x.OESGroupId) &&
                             x.ItemBank.ItemBankSignature == rootItemBank.ItemBankSignature, Including: nameof(ItemBankGroups.ItemBank), asNoTracking: true);

            foreach (var itemBank in itemBanks)
            {
                var userItemBank = new UserItemBankDto
                {
                    Id = itemBank.ItemBankId,
                    Name = itemBank.ItemBank.Name,
                    Code = itemBank.ItemBank.Code,
                };

                userItemBanksList.Add(userItemBank);
            }

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              Resource.SuccessfulFetching,
                                                              userItemBanksList);
        }


        public async Task<ApiResponse> GetAllItemBankRootsForPaperAsync(PaginationSearchModel pagination)
        {
            ItemBankFilterDto itemBankFilter = null;

            if (pagination.FilterObj is JsonElement jsonElement)
            {
                itemBankFilter = jsonElement.Deserialize<ItemBankFilterDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            IQueryable<ItemBank> query;

            if (_filterParamsValues.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin) ||
                _filterParamsValues.SsoUserRoles.Exists(x => x.Name == AdminRoles.Entity_Admin))
            {
                query = _commonService
                    ._unitOfWork
                    .Repository<ItemBank, long>()
                    .GetAll(e => e.ParentId == null || e.ParentId == 0,
                        Including:
                            $"{nameof(ItemBank.QuestionMetadata)}," +
                            $"{nameof(ItemBank.Childreen)}.{nameof(ItemBank.QuestionMetadata)}"
                    )
                    .AsNoTracking();
            }
            else
            {
                var userGroupIds = _filterParamsValues
                    .OesUserGroupsAndRoles
                    .Select(g => g.GroupId)
                    .Distinct()
                    .ToList();

                query = _commonService
                    ._unitOfWork
                    .Repository<ItemBank, long>()
                    .Query()
                    .Where(e =>
                        (e.ParentId == null || e.ParentId == 0) &&
                        e.ItemBankGroups.Any(ibg => userGroupIds.Contains(ibg.OESGroupId) && !ibg.IsDeleted)
                    )
                    .Include(nameof(ItemBank.QuestionMetadata))
                    .Include($"{nameof(ItemBank.Childreen)}.{nameof(ItemBank.QuestionMetadata)}")
                    .AsNoTracking();
            }

            if (!pagination.PaginationOff)
            {
                if (!string.IsNullOrEmpty(pagination.SearchKey))
                {
                    if (pagination.SearchInName && pagination.SearchInDescription)
                    {
                        query = query.Where(x =>
                            x.Name.Contains(pagination.SearchKey) ||
                            x.Description.Contains(pagination.SearchKey));
                    }
                    else if (pagination.SearchInName)
                    {
                        query = query.Where(x => x.Name.Contains(pagination.SearchKey));
                    }
                    else if (pagination.SearchInDescription)
                    {
                        query = query.Where(x => x.Description.Contains(pagination.SearchKey));
                    }
                }

                if (pagination.FromDate is not null)
                {
                    query = query.Where(o =>
                        o.CreationDate >= pagination.FromDate &&
                        o.CreationDate <= (pagination.ToDate ?? DateTimeHelper.Now.Date));
                }

                query = pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

                var totalItems = await query.CountAsync();

                var data = await query
                    .Skip(pagination.PageIndex * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var mappedData = await MapItemBanksWithQuestionCountAsync(data, itemBankFilter);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    new CustomTableData<NewItemBankDTO>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await query.ToListAsync();

                var mappedData = await MapItemBanksWithQuestionCountAsync(data, itemBankFilter);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SuccessfulFetching,
                    new CustomTableData<NewItemBankDTO>(mappedData, data.Count)
                );
            }
        }

        public async Task<ApiResponse> TransferQuestionsToItemBankAsync(TransferQuestionsToItemBankDto dto)
        {
            var flattenedItemBanks = await GetFlattenedItemBanksForTransferAsync(dto.SourceItemBankId, dto.TransferType);

            var paperBoundItemBankNames = new List<string>();

            if (dto.TransferType == ItemBankQuestionsTransferType.WithChilds)
            {
                foreach (var itemBank in flattenedItemBanks)
                {
                    var linkedPapersCodes = await GetLinkedPapersCodesAsync(itemBank.Id);

                    if (linkedPapersCodes?.Count > 0)
                    {
                        paperBoundItemBankNames.Add(itemBank.Name);
                    }
                }

                if (paperBoundItemBankNames.Count > 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        string.Format(Resource.FailedToTransferItemBanksQuestionsBecauseTheyLinkedToPapers, string.Join(", ", paperBoundItemBankNames)));
                }
            }
            else if (dto.TransferType == ItemBankQuestionsTransferType.WithoutChilds)
            {
                var linkedPapersCodes = await GetLinkedPapersCodesAsync(dto.SourceItemBankId);

                if (linkedPapersCodes?.Count > 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Conflict,
                                        HttpStatusCode.Conflict,
                                        string.Format(Resource.FailedToTransferSelectedItemBankQuestionsBecauseItLinkedToPapers));
                }
            }

            var allItemBankIds = flattenedItemBanks.ConvertAll(x => x.Id);

            var hostItemBankUnscored = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query()
                .Where(x => x.Id == dto.HostItemBankId)
                .Select(x => x.Unscored)
                .FirstOrDefaultAsync();

            var updatedQuestionsCount = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(x => allItemBankIds.Contains(x.ItemBankId))
                .ExecuteUpdateAsync(x => x.SetProperty(q => q.ItemBankId, dto.HostItemBankId).SetProperty(q => q.Unscored, hostItemBankUnscored));

            if (updatedQuestionsCount > 0)
            {
                return _commonService
                      ._apiResponse
                      .GetApiResponse(CustomCodeStatus.Success,
                                      HttpStatusCode.OK,
                                      Resource.QuestionsTransferredSuccessfully);
            }
            else if (updatedQuestionsCount == 0)
            {
                return _commonService
                     ._apiResponse
                     .GetApiResponse(CustomCodeStatus.Success,
                                     HttpStatusCode.NotModified,
                                     Resource.NoQuestionsToTransfer);
            }
            else
            {
                return _commonService
                     ._apiResponse
                     .GetApiResponse(CustomCodeStatus.InternalServerError,
                                     HttpStatusCode.InternalServerError,
                                     Resource.QuestionsTransferFailed);
            }
        }

        public async Task<ApiResponse> CanDoQuestionActionAsync(long itemBankId, string requiredRole)
        {
            return await _itemBankAuthorizationService.CanDoQuestionActionAsync(itemBankId, requiredRole);
        }


        #region Template Methods

        public async Task<ApiResponse> AddTemplateAIItemBankAsync(AIItemBankTemplateCreationDto dto)
        {
            var isNameExists = await _commonService
                ._unitOfWork
                .Repository<ItemBankTemplate, long>()
                .IsExistAsync(q => q.Name.ToLower() == dto.Name.ToLower());

            if (isNameExists)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Failure,
                                    HttpStatusCode.Conflict,
                                    Resource.TemplateWithTheSameNameAlreadyExists);
            }

            var jsonObject = JsonConvert.SerializeObject(dto);

            ItemBankTemplate template = new()
            {
                Name = dto.Name,
                Data = jsonObject,
                IsFromItmBankAI = dto.IsFromAI
            };

            await _commonService
                ._unitOfWork
                .Repository<ItemBankTemplate, long>()
                .AddAsync(template);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.TemplateAddedSuccessfully,
                                    template.Id);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.SomethingWentWrong);
        }

        public async Task<ApiResponse> GetAllItemBankTemplatesAsync(PaginationSearchModel paginationSearch, bool isFromItmBankAI)
        {
            var query = _commonService._unitOfWork.Repository<ItemBankTemplate, long>().GetAll().AsQueryable();

            if (isFromItmBankAI)
            {
                query = query.Where(q => q.IsFromItmBankAI == isFromItmBankAI);
            }

            if (!string.IsNullOrEmpty(paginationSearch.SearchKey) && paginationSearch.SearchInName)
            {
                var searchKeyLower = paginationSearch.SearchKey.ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(searchKeyLower));
            }

            if (paginationSearch.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearch.FromDate.Value);
            }

            if (paginationSearch.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearch.ToDate.Value);
            }

            query = paginationSearch.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(x => x.CreationDate)
                : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoTemplatesFound);
            }

            var pageIndex = Math.Max(0, paginationSearch.PageIndex);
            var pageSize = Math.Max(1, paginationSearch.PageSize);

            var paginatedTemplates = await query.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

            var dtoList = paginatedTemplates.ConvertAll(t => new ItemBankTemplateDto
            {
                Id = t.Id,
                Name = t.Name
            });

            var responseData = new CustomTableData<ItemBankTemplateDto>(dtoList, totalRecords);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, responseData);
        }

        public async Task<ApiResponse> GetItemBankTemplateByIdAsync(long templateId)
        {
            var template = await _commonService._unitOfWork.Repository<ItemBankTemplate, long>().GetByIdAsync(templateId);

            if (template == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);
            }
            var deserializedData = JsonConvert.DeserializeObject<AIItemBankTemplateCreationDto>(template.Data);

            if (deserializedData == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.InternalServerError, Resource.InvalidDataFormat);
            }

            deserializedData.Name = template.Name;

            GetAIItemBankTemplateResponseDto responseDto = new()
            {
                AIItemBankTemplate = deserializedData
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null, responseDto);
        }

        public async Task<ApiResponse> DeleteItemBankTemplateAsync(long templateId)
        {
            var template = await _commonService._unitOfWork.Repository<ItemBankTemplate, long>().GetByIdAsync(templateId);

            if (template == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.TemplateNotFound);
            }

            _commonService._unitOfWork.Repository<ItemBankTemplate, long>().Delete(template);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.TemplateDeletedSuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.SomethingWentWrong);
        }

        #endregion


        #region Helper Methods

        private async Task<List<NewItemBankDTO>> MapItemBanksWithQuestionCountAsync(List<ItemBank> itemBanks, ItemBankFilterDto filter = null)
        {
            var mappedData = _mapper.Map<List<NewItemBankDTO>>(itemBanks);

            if (mappedData.Count == 0) return mappedData;

            //if (filter != null && filter.PaperId > 0)
            //{
            //    await SetTotalQuestionsWithFilterAsync(itemBanks, mappedData, filter);
            //}
            //else
            //{

            var itemBankIds = itemBanks.ConvertAll(x => x.Id);

            var summaries = await _commonService
                ._unitOfWork
                .Repository<ItemBanksSummaryReportView, int>()
                .Query()
                .AsNoTracking()
                .Where(v => itemBankIds.Contains(v.Id))
                .ToListAsync();

            foreach (var dto in mappedData)
            {
                dto.TotalQuestions = summaries
                    .Where(s => s.Id == dto.Id)
                    .Select(s => s.TotalQuestions)
                    .FirstOrDefault();
            }

            //}

            return mappedData;
        }

        private static bool IsDescendant(ItemBank parent, ItemBank child)
        {
            if (parent.Id == child.Id)
            {
                return true;
            }

            if (parent.Childreen != null)
            {
                foreach (var childNode in parent.Childreen)
                {
                    if (IsDescendant(childNode, child))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<List<ItemBank>> GetFlattenedItemBanksForTransferAsync(long targetParentItemBankId, ItemBankQuestionsTransferType transferType)
        {
            var targetParentItemBank = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .GetObjAsync(x => x.Id == targetParentItemBankId);

            List<ItemBank> accumulativeItemBankList = [];

            accumulativeItemBankList.Add(targetParentItemBank);

            if (transferType == ItemBankQuestionsTransferType.WithChilds)
            {
                var targetParentItemBankDescendants = await GetNestedNodesOfItemBankAsync(targetParentItemBankId);

                accumulativeItemBankList.AddRange(targetParentItemBankDescendants);
            }

            return accumulativeItemBankList;
        }

        private async Task SetTotalQuestionsWithFilterAsync(
            List<ItemBank> itemBanks,
            List<NewItemBankDTO> mappedData,
            ItemBankFilterDto filter)
        {
            var paperSubjectIds = await _commonService
                ._unitOfWork
                .Repository<PaperSubject, long>()
                .Query()
                .AsNoTracking()
                .Where(ps => ps.PaperId == filter.PaperId)
                .Select(ps => ps.SubjectId)
                .ToListAsync();

            var itemBankSignatures = itemBanks
                .Select(x => x.ItemBankSignature)
                .Distinct()
                .ToList();

            var allRelatedIds = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query()
                .AsNoTracking()
                .Where(ib => itemBankSignatures.Contains(ib.ItemBankSignature))
                .Select(ib => new { ib.Id, ib.ItemBankSignature })
                .ToListAsync();

            var signatureToItemBankIds = allRelatedIds
                .GroupBy(x => x.ItemBankSignature)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

            var baseQuery = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(qm => allRelatedIds.Select(x => x.Id).Contains(qm.ItemBankId) && qm.ParentId == null);

            if (paperSubjectIds.Count > 0)
            {
                baseQuery = baseQuery.Where(qm => qm.SubjectId != null && paperSubjectIds.Contains((long)qm.SubjectId));
            }

            if (filter.DifficultyProfileId.HasValue)
            {
                baseQuery = baseQuery.Where(qm => qm.DifficultyProfileId == filter.DifficultyProfileId.Value);
            }

            if (filter.LanguageId > 0)
            {
                baseQuery = baseQuery.Where(qm => qm.QuestionDetails.Any(qd => qd.LanguageId == filter.LanguageId));
            }

            var countPerItemBank = await baseQuery
                .GroupBy(qm => qm.ItemBankId)
                .Select(g => new { ItemBankId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ItemBankId, x => x.Count);

            foreach (var dto in mappedData)
            {
                var signature = itemBanks.First(x => x.Id == dto.Id).ItemBankSignature;

                if (!signatureToItemBankIds.TryGetValue(signature, out var relatedIds))
                {
                    dto.TotalQuestions = 0;
                    continue;
                }

                dto.TotalQuestions = relatedIds
                    .Where(id => countPerItemBank.ContainsKey(id))
                    .Sum(id => countPerItemBank[id]);
            }
        }

        private async Task CascadeUnscoredToDescendantsAsync(long itemBankId, bool unscored)
        {
            var questionMetaRepo = _commonService._unitOfWork.Repository<QuestionMetadata, long>();
            var itemBankRepo = _commonService._unitOfWork.Repository<ItemBank, long>();

            await questionMetaRepo
                .Query()
                .Where(x => x.ItemBankId == itemBankId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Unscored, unscored));

            var descendants = await GetNestedNodesOfItemBankAsync(itemBankId);

            if (!descendants.Any())
                return;

            var descendantIds = descendants.ConvertAll(x => x.Id);

            await itemBankRepo
                .Query()
                .Where(x => descendantIds.Contains(x.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Unscored, unscored));

            await questionMetaRepo
                .Query()
                .Where(x => descendantIds.Contains(x.ItemBankId))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Unscored, unscored));
        }

        #region Item Bank Tree Save Helpers

        private async Task AttachChildrenRecursivelyAsync(AIItemBankNodeDto parentNode, ItemBank parentEntity, Dictionary<string, long> levelIdCache)
        {
            if (parentNode.Children == null || parentNode.Children.Count == 0)
            {
                return;
            }

            parentEntity.Childreen ??= [];

            // All children of this parent are one sibling group -> one shared Level.
            var sharedLevelId = await ResolveSharedLevelIdAsync(parentNode.Children, parentEntity.OrganizationSignature, levelIdCache);

            foreach (var childNode in parentNode.Children)
            {
                var childEntity = BuildEntity(childNode, parentEntity, sharedLevelId);

                parentEntity.Childreen.Add(childEntity);

                await AttachChildrenRecursivelyAsync(childNode, childEntity, levelIdCache);
            }
        }

        private async Task<long> ResolveSharedLevelIdAsync(List<AIItemBankNodeDto> siblingGroup, string? organizationSignature, Dictionary<string, long> levelIdCache)
        {
            var firstNode = siblingGroup[0];
            var levelName = firstNode.Level?.Trim() ?? string.Empty;
            var normalizedLevelName = levelName.ToLower();

            var cacheKey = $"{organizationSignature}::{normalizedLevelName}";

            if (levelIdCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            var existingLevel = await _commonService
                ._unitOfWork
                .Repository<ItemBankLevel, long>()
                .GetObjAsync(l => l.Name.ToLower() == normalizedLevelName && l.OrganizationSignature == organizationSignature);

            if (existingLevel != null)
            {
                levelIdCache[cacheKey] = existingLevel.Id;
                return existingLevel.Id;
            }

            var newLevel = new ItemBankLevel
            {
                Name = levelName,
                OrganizationSignature = organizationSignature
            };

            await _commonService._unitOfWork.Repository<ItemBankLevel, long>().AddAsync(newLevel);

            await _commonService._unitOfWork.Complete();

            levelIdCache[cacheKey] = newLevel.Id;

            return newLevel.Id;
        }

        private ItemBank BuildEntity(AIItemBankNodeDto node, ItemBank? parentEntity, long levelId)
        {
            return new ItemBank
            {
                Name = node.Name?.Trim(),
                Code = node.Code?.Trim(),
                Description = node.Description,
                LevelId = levelId,
                Unscored = node.Unscored,
                IsActive = node.IsActive,
                ItemBankSignature = parentEntity?.ItemBankSignature ?? RandomGenerator.GenerateItemBankSignatureCode(node.Name ?? string.Empty),
                OrganizationId = _filterParamsValues.OrganizationId,
                ItemBankGroups = BuildInheritedItemBankGroups(parentEntity, node.OESGroupDtos)
            };
        }

        private static List<ItemBankGroups> BuildInheritedItemBankGroups(ItemBank? parentEntity, List<Helper.Dtos.OESUserGroups.GetOESGroupDto>? requestedGroups)
        {
            var groups = new List<ItemBankGroups>();

            if (parentEntity?.ItemBankGroups != null)
            {
                var ownerGroupId = parentEntity
                    .ItemBankGroups
                    .FirstOrDefault(g => g.OESGroup != null && g.OESGroup.AutoCreatedForUser)?
                    .OESGroupId;

                if (ownerGroupId != null)
                {
                    groups.Add(new ItemBankGroups { OESGroupId = ownerGroupId.Value });
                }
            }

            if (requestedGroups != null)
            {
                foreach (var g in requestedGroups)
                {
                    if (!groups.Any(x => x.OESGroupId == g.Id))
                    {
                        groups.Add(new ItemBankGroups { OESGroupId = g.Id });
                    }
                }
            }

            return groups;
        }

        #endregion

        #endregion Helper Methods
    }
}