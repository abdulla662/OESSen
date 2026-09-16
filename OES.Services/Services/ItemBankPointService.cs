using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.ItemBankPoint.Requests;
using OES.Helper.Dtos.ItemBankPoint.Responses;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class ItemBankPointService : IItemBankPointService
    {
        private readonly IPaperService _paperService;
        private readonly ICommonService _commonService;

        public ItemBankPointService(IPaperService paperService, ICommonService commonService)
        {
            _paperService = paperService;
            _commonService = commonService;
        }

        public async Task<IApiResponse> GetPaperSelectedItemBanksAsync(long paperId)
        {
            var itemBankPoints = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(result => result.PaperId == paperId, Including: nameof(PaperItemBankPoint.ItemBank))
                .ToListAsync();

            IEnumerable<IEnumerable<TreeItemResponseDto>> treeItemResponseDtos = [.. itemBankPoints
                .GroupBy(x => x.ItemBank.ItemBankSignature)
                .Select(group => group.Select(item => new TreeItemResponseDto
                {
                    Id = item.ItemBank.Id,
                    Text = item.ItemBank.Name
                }))
            ];

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.PaperItemBanksRetrievedSuccessfully,
                                treeItemResponseDtos);
        }

        public async Task<ApiResponse> AddItemBankPointAsync(AddOrUpdateItemBankPointRequestDto addItemBankPointRequestDto)
        {
            var validationResult = await ValidateItemBankToPaperQuestionCountMatchAsync(addItemBankPointRequestDto);

            if (validationResult.ExpectedTotalQuestionsNeeded == -1)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.InsufficientItemBankQuestions
                );
            }

            if (!validationResult.QuestionsCountEnoughForAllForms)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    string.Format(
                        Resource.InsufficientItemBankQuestions,
                        validationResult.SelectedItemBanksQuestionsCount,
                        validationResult.ExpectedTotalQuestionsNeeded
                    )
                );
            }

            var existedItemBankPoints = await _commonService
               ._unitOfWork
               .Repository<PaperItemBankPoint, long>()
               .GetAllAsync(x => x.PaperId == addItemBankPointRequestDto.PaperId, Including: $"{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)},{nameof(PaperItemBankPoint.AutoPaperItemBankQuestionSections)}");

            var newItemBanksIdsToBeAdded = addItemBankPointRequestDto.ItemBankIds.ExceptBy(existedItemBankPoints.Select(x => x.ItemBankId), x => x).ToList();

            List<PaperItemBankPoint> itemBankPoints = [];

            foreach (var itemBankId in newItemBanksIdsToBeAdded)
            {
                itemBankPoints.Add(new PaperItemBankPoint
                {
                    ItemBankId = itemBankId,
                    PaperId = addItemBankPointRequestDto.PaperId
                });
            }

            _commonService._unitOfWork.Repository<PaperItemBankPoint, long>().AddRangAsync(itemBankPoints);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                var updatePaperCreationStatusDto = new UpdatePaperCreationStatusRequestDto(addItemBankPointRequestDto.PaperId, PaperCreationStatus.ItemBanksOrBlocksSelected);

                await _paperService.UpdatePaperCreationStatusAsync(updatePaperCreationStatusDto);

                var result = itemBankPoints.ConvertAll(x => new PaperItemBankPointDto(x.Id, x.PaperId, x.ItemBankId));

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.SelectedItemBankPointsAddedSuccessfully,
                    result
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.BadRequest,
                Resource.SomethingWentWrong
            );
        }

        public async Task<IApiResponse> UpdateItemBankPointAsync(AddOrUpdateItemBankPointRequestDto editItemBankPointRequestDto)
        {
            var validationResult = await ValidateItemBankToPaperQuestionCountMatchAsync(editItemBankPointRequestDto);

            if (validationResult.ExpectedTotalQuestionsNeeded == -1)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.InsufficientQuestions
                );
            }

            if (!validationResult.QuestionsCountEnoughForAllForms)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    string.Format(
                        Resource.NotEnoughQuestions,
                        validationResult.SelectedItemBanksQuestionsCount,
                        validationResult.ExpectedTotalQuestionsNeeded
                    )
                );
            }

            var existedItemBankPoints = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAllAsync(x => x.PaperId == editItemBankPointRequestDto.PaperId, Including: $"{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)},{nameof(PaperItemBankPoint.AutoPaperItemBankQuestionSections)}");

            var newItemBanksIdsToBeAdded = editItemBankPointRequestDto.ItemBankIds.ExceptBy(existedItemBankPoints.Select(x => x.ItemBankId), x => x).ToList();

            var existedItemBanksIdsToBeDeleted = existedItemBankPoints.Select(x => x.ItemBankId).ExceptBy(editItemBankPointRequestDto.ItemBankIds, x => x).ToList();

            if (newItemBanksIdsToBeAdded.Count > 0)
            {
                var itemBankPoints = newItemBanksIdsToBeAdded.ConvertAll(x => new PaperItemBankPoint
                {
                    ItemBankId = x,
                    PaperId = editItemBankPointRequestDto.PaperId
                });

                _commonService
                    ._unitOfWork
                    .Repository<PaperItemBankPoint, long>()
                    .AddRangAsync(itemBankPoints);
            }

            if (existedItemBanksIdsToBeDeleted.Count > 0)
            {
                var existedItemBankPointsToBeDeleted = existedItemBankPoints.Where(x => existedItemBanksIdsToBeDeleted.Contains(x.ItemBankId));

                foreach (var itemBankPoint in existedItemBankPointsToBeDeleted)
                {
                    itemBankPoint.ManualPaperItemBankQuestionSections.Clear();
                    itemBankPoint.AutoPaperItemBankQuestionSections.Clear();
                }

                _commonService
                    ._unitOfWork
                    .Repository<PaperItemBankPoint, long>()
                    .DeleteRange(existedItemBankPointsToBeDeleted);
            }

            await _commonService._unitOfWork.Complete();

            var updatePaperCreationStatusDto = new UpdatePaperCreationStatusRequestDto(editItemBankPointRequestDto.PaperId, PaperCreationStatus.ItemBanksOrBlocksSelected);

            await _paperService.UpdatePaperCreationStatusAsync(updatePaperCreationStatusDto);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperItemBanksUpdatedSuccessfully
            );
        }


        #region Validation Methods

        private async Task<(bool QuestionsCountEnoughForAllForms, long SelectedItemBanksQuestionsCount, int PaperQuestionsCount, long ExpectedTotalQuestionsNeeded)> ValidateItemBankToPaperQuestionCountMatchAsync(AddOrUpdateItemBankPointRequestDto addItemBankPointRequestDto)
        {
            var allAvailableQuestions = await _commonService
                ._unitOfWork
                .Repository<ItemBank, long>()
                .Query()
                .Include(q => q.QuestionMetadata)
                    .ThenInclude(QuestionMetadata => QuestionMetadata.QuestionDetails)
                .Include(q => q.QuestionMetadata)
                    .ThenInclude(QuestionMetadata => QuestionMetadata.QuestionType)
                .Include(q => q.QuestionMetadata)
                    .ThenInclude(qm => qm.SubQuestions)
                .AsNoTracking()
                .Where(x => addItemBankPointRequestDto.ItemBankIds.Contains(x.Id))
                .SelectMany(x => x.QuestionMetadata
                    .Where(y => y.DifficultyProfileId == addItemBankPointRequestDto.PaperDifficultyProfileId &&
                                y.QuestionStatus == QuestionStatus.Approved &&
                                y.CurrentExhaustionCount < y.QuestionsExhaustionCount &&
                                (y.IsRoot || y.ParentId == null) &&
                                y.QuestionDetails.Any(z => z.LanguageId == addItemBankPointRequestDto.PaperLanguageId) &&
                                (!addItemBankPointRequestDto.AllowInstantResult || y.QuestionType.IsAutoCorrectable)))
                .ToListAsync();

            var totalQuestionsCountOfSelectedItemBanks = allAvailableQuestions
                .Sum(q => q.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension ? q.SubQuestions.Count : 1);

            var paperQuestionsCount = addItemBankPointRequestDto.PaperQuestionsCount;
            var outputFormsCount = addItemBankPointRequestDto.OutputFormsCount;

            int expectedTotalQuestions;

            if (addItemBankPointRequestDto.QuestionSelectionType == QuestionSelectionType.Manual)
            {
                // For now, this validation only applies for the LAST added form:

                expectedTotalQuestions = paperQuestionsCount * 1; // 1 here indicates the Last form

                var questionsCountEnoughForAllForms = totalQuestionsCountOfSelectedItemBanks >= expectedTotalQuestions;

                return (questionsCountEnoughForAllForms,
                        totalQuestionsCountOfSelectedItemBanks,
                        addItemBankPointRequestDto.PaperQuestionsCount,
                        expectedTotalQuestions);
            }
            else if (addItemBankPointRequestDto.QuestionSelectionType == QuestionSelectionType.Auto)
            {
                // At least one combination of (difficulty level and question type) in the available questions must be >= forms count:
                var atLeastOneComboEnough = allAvailableQuestions
                    .SelectMany(q => q.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension ? q.SubQuestions.Select(sq => new { sq.DifficultyLevelId, sq.QuestionTypeId }) : [new { q.DifficultyLevelId, q.QuestionTypeId }])
                    .GroupBy(q => new { q.DifficultyLevelId, q.QuestionTypeId })
                    .Any(g => g.Count() >= outputFormsCount);

                if (addItemBankPointRequestDto.QuestionDistributionTypeInForm == QuestionDistributionTypeInForm.PartiallyDistinct)
                {
                    if (atLeastOneComboEnough)
                    {
                        expectedTotalQuestions = paperQuestionsCount + 1;

                        var questionsCountEnoughForAllForms = totalQuestionsCountOfSelectedItemBanks >= expectedTotalQuestions;

                        return (questionsCountEnoughForAllForms,
                                totalQuestionsCountOfSelectedItemBanks,
                                addItemBankPointRequestDto.PaperQuestionsCount,
                                expectedTotalQuestions);
                    }
                    else
                    {
                        return (
                            false, // QuestionsCountEnoughForAllForms
                            totalQuestionsCountOfSelectedItemBanks,
                            addItemBankPointRequestDto.PaperQuestionsCount,
                            -1 // ExpectedTotalQuestionsNeeded
                        );
                    }
                }
                else
                {
                    // Fully distinct questions for each form:
                    expectedTotalQuestions = (int)(paperQuestionsCount * outputFormsCount);

                    var questionsCountEnoughForAllForms = totalQuestionsCountOfSelectedItemBanks >= expectedTotalQuestions;

                    return (
                        questionsCountEnoughForAllForms,
                        totalQuestionsCountOfSelectedItemBanks,
                        addItemBankPointRequestDto.PaperQuestionsCount,
                        expectedTotalQuestions
                    );
                }
            }

            return (false,
                    totalQuestionsCountOfSelectedItemBanks,
                    addItemBankPointRequestDto.PaperQuestionsCount,
                    -1);
        }

        #endregion
    }
}