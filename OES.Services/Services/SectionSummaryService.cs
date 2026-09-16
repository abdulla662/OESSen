using OES.Core.Entities.Paper.Views;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Paper.Responses.AdaptiveSummary;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Interface.Interfaces;
using SharedHelper.Enums;
using System.Net;

namespace OES.Services.Services
{
    public class SectionSummaryService(ICommonService _commonService) : ISectionSummaryService
    {
        public async Task<ApiResponse> GetSectionSummaryAsync(long paperId, QuestionSelectionType questionSelectionType, PaperType paperType)
        {

            if (questionSelectionType == QuestionSelectionType.Manual && paperType == PaperType.Standard)
            {
                return await GetSectionSummaryManual(paperId);
            }
            else if (questionSelectionType == QuestionSelectionType.Auto && paperType == PaperType.Standard)
            {
                return await GetSectionSummaryAuto(paperId);
            }
            else if (paperType == PaperType.Adaptive)
            {
                return await GetPaperStageSectionBlockSummary(paperId);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    "Invalid question selection type"
                );
            }
        }

        private async Task<ApiResponse> GetSectionSummaryAuto(long paperId)
        {
            var autoQuestionsSummary = (await _commonService
                ._unitOfWork
                .Repository<AutoQuestionSummaryView, long>()
                .GetAllAsync(q => q.PaperId == paperId))
                .ToList();

            if (autoQuestionsSummary.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "No questions found for required paper"
                );
            }

            var results = autoQuestionsSummary
                .GroupBy(q => q.PaperId)
                .Select(paperGroup => new StandardAutoPaperSummaryResponseDto
                {
                    PaperId = paperGroup.Key,
                    PaperName = paperGroup.First().PaperName,
                    NumberOfSection = paperGroup.Select(q => q.SectionId).Distinct().Count(),
                    NumberOfQuestion = paperGroup.Sum(q => q.QuestionsCount),
                    NumberOfItemBanks = paperGroup.Select(q => q.ItemBankId).Distinct().Count(),
                    SectionSummaryDtos = [.. paperGroup
                        .GroupBy(q => new { q.SectionId, q.SectionName, q.SectionOrderId })
                        .OrderBy(g => g.Key.SectionOrderId)
                        .Select(sectionGroup => new SectionSummaryResponseDto
                        {
                            SectionId = sectionGroup.Key.SectionId,
                            SectionName = sectionGroup.Key.SectionName,
                            OrderId = sectionGroup.Key.SectionOrderId,
                            NumberOfQuestion = sectionGroup.Sum(q => q.QuestionsCount),
                            SectionItemBankSummaryResponseDtos = [.. sectionGroup
                                .GroupBy(q => new { q.ItemBankId, q.ItemBankName })
                                .Select(itembankGroup => new SectionItemBankSummaryResponseDto
                                {
                                    ItemBankId = itembankGroup.Key.ItemBankId,
                                    ItemBankName = itembankGroup.Key.ItemBankName,
                                    DifficultyLevelSummaryResponseDtos = [.. itembankGroup
                                        .GroupBy(q => new { q.DifficultyLevelId, q.DifficultyLevelName })
                                        .Select(difficultyGroup => new DifficultyLevelSummaryResponseDto
                                        {
                                            DifficultyLevelId = difficultyGroup.Key.DifficultyLevelId,
                                            DifficultyLevelName = difficultyGroup.Key.DifficultyLevelName,
                                            NumberOfQuestion = difficultyGroup.Sum(q => q.QuestionsCount),
                                            QuestionTypeResponseDtos = [.. difficultyGroup
                                                .GroupBy(q => new { q.QuestionTypeId, q.QuestionTypeName })
                                                .Select(questionTypeGroup => new QuestionTypeResponseDto
                                                {
                                                    QuestionTypeId = questionTypeGroup.Key.QuestionTypeId,
                                                    QuestionTypeName = questionTypeGroup.Key.QuestionTypeName,
                                                    NumberOfQuestions = questionTypeGroup.Sum(q => q.QuestionsCount)
                                                })]
                                        })]
                                })]
                        })]
                })
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Question summary for required paper retrieved successfully",
                results.FirstOrDefault()
            );
        }

        private async Task<ApiResponse> GetSectionSummaryManual(long paperId)
        {
            var manualQuestionsSummary = (await _commonService
                ._unitOfWork
                .Repository<ManualQuestionSummaryView, long>()
                .GetAllAsync(q => q.PaperId == paperId))
                .ToList();

            if (manualQuestionsSummary.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "No questions found for required paper"
                );
            }

            var results = manualQuestionsSummary
                .GroupBy(q => q.PaperId)
                .Select(paperGroup => new StandardManualPaperSummaryResponseDto
                {
                    PaperId = paperGroup.Key,
                    PaperName = paperGroup.First().PaperName,
                    NumberOfSection = paperGroup.Select(q => q.SectionId).Distinct().Count(),
                    NumberOfQuestion = paperGroup.Sum(q => q.QuestionsCount),
                    NumberOfItemBanks = paperGroup.Select(q => q.ItemBankId).Distinct().Count(),
                    FormSummaryResponseDtos = [.. paperGroup
                        .GroupBy(q => new { q.FormId, q.FormName })
                        .Select(formGroup => new FormSummaryResponseDto
                        {
                            FormId = formGroup.Key.FormId,
                            FormName = formGroup.Key.FormName,
                            SectionSummaryDtos = [.. formGroup
                                .GroupBy(q => new { q.SectionId, q.SectionName, q.SectionOrderId })
                                .OrderBy(g => g.Key.SectionOrderId)
                                .Select(sectionGroup => new SectionSummaryResponseDto
                                {
                                    SectionId = sectionGroup.Key.SectionId,
                                    SectionName = sectionGroup.Key.SectionName,
                                    OrderId = sectionGroup.Key.SectionOrderId,
                                    NumberOfQuestion = sectionGroup.Sum(q => q.QuestionsCount),
                                    SectionItemBankSummaryResponseDtos = [.. sectionGroup
                                        .GroupBy(q => new { q.ItemBankId, q.ItemBankName })
                                        .Select(itembankGroup => new SectionItemBankSummaryResponseDto
                                        {
                                            ItemBankId = itembankGroup.Key.ItemBankId,
                                            ItemBankName = itembankGroup.Key.ItemBankName,
                                            DifficultyLevelSummaryResponseDtos = [.. itembankGroup
                                                .GroupBy(q => new { q.DifficultyLevelId, q.DifficultyLevelName })
                                                .Select(difficultyGroup => new DifficultyLevelSummaryResponseDto
                                                {
                                                    DifficultyLevelId = difficultyGroup.Key.DifficultyLevelId,
                                                    DifficultyLevelName = difficultyGroup.Key.DifficultyLevelName,
                                                    NumberOfQuestion = difficultyGroup.Sum(q => q.QuestionsCount),
                                                    QuestionTypeResponseDtos = [.. difficultyGroup
                                                        .GroupBy(q => new { q.QuestionTypeId, q.QuestionTypeName })
                                                        .Select(questionTypeGroup => new QuestionTypeResponseDto
                                                        {
                                                            QuestionTypeId = questionTypeGroup.Key.QuestionTypeId,
                                                            QuestionTypeName = questionTypeGroup.Key.QuestionTypeName,
                                                            NumberOfQuestions = questionTypeGroup.Sum(q => q.QuestionsCount)
                                                        })]
                                                })]
                                        })]
                                })]
                        })]
                })
                .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Question summary for required paper retrieved successfully",
                results.FirstOrDefault()
            );
        }

        private async Task<ApiResponse> GetPaperStageSectionBlockSummary(long paperId)
        {
            var summaryData = (await _commonService
                ._unitOfWork
                .Repository<PaperStageSectionBlockSummaryView, long>()
                .GetAllAsync(x => x.PaperId == paperId))
                .ToList();

            if (summaryData.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "No data found for paper with this id"
                );
            }

            var result = new PaperStageSummaryResponseDto
            {
                PaperId = paperId,
                PaperName = summaryData.FirstOrDefault()?.PaperName,
                NumberOfQuestion = summaryData.Sum(x => x.QuestionCount),
                NumberOfSection = summaryData.Select(x => x.SectionId).Distinct().Count(),
                NumberOfStages = summaryData.Select(x => x.StageId).Distinct().Count(),
                Stages = [.. summaryData
                    .GroupBy(s => new { s.StageId, s.StageName, s.StageOrder, s.StageTime })
                    .OrderBy(g => g.Key.StageOrder)
                    .Select(stageGroup => new StageSummaryDto
                    {
                        StageId = stageGroup.Key.StageId,
                        StageName = stageGroup.Key.StageName,
                        StageOrder = stageGroup.Key.StageOrder,
                        StageTime = stageGroup.Key.StageTime,
                        Sections = [.. stageGroup
                            .GroupBy(sec => new { sec.SectionId, sec.SectionName, sec.SectionOrder })
                            .OrderBy(g => g.Key.SectionOrder)
                            .Select(sectionGroup => new SectionSummaryDto
                            {
                                SectionId = sectionGroup.Key.SectionId,
                                SectionName = sectionGroup.Key.SectionName,
                                SectionOrder = sectionGroup.Key.SectionOrder,
                                Blocks = [.. sectionGroup
                                    .Where(x => x.BlockId != null)
                                    .Select(block => new BlockSummaryDto
                                    {
                                        BlockId = block.BlockId,
                                        BlockName = block.BlockName,
                                        BlockCode = block.BlockCode,
                                        QuestionCount = block.QuestionCount
                                    })
                                ]
                            })
                        ]
                    })
                ]
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Paper summary retrieved successfully",
                result
            );
        }
    }
}
