using Microsoft.EntityFrameworkCore;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Paper.Views;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QuestionDistributionValidationService : IQuestionDistributionValidationService
    {
        private readonly ICommonService _commonService;
        private readonly IQuestionService _questionService;

        public QuestionDistributionValidationService(ICommonService commonService, IQuestionService questionService)
        {
            _commonService = commonService;
            _questionService = questionService;
        }

        public async Task<ApiResponse> ValidateQuestionTypeAndDifficultyDistributionAsync(AddOrUpdateAutoSectionsDistributionsRequestDto request)
        {
            var manualValidationResponse = await ValidateManualQuestions(request);
            if (manualValidationResponse.StatusCode != HttpStatusCode.OK)
            {
                return manualValidationResponse;
            }

            var flattenedNodes = await _commonService
                ._unitOfWork
                .Repository<FlattenedTreeNodeView, long>()
                .GetAll(o => o.PaperId == request.PaperId)
                .ToListAsync();

            var questionTypeAndDifficultyDistributionValidationResultDto = new QuestionTypeAndDifficultyDistributionValidationResultDto(false);

            if (flattenedNodes.Count == 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.Flattenedtreenodesnotfound,
                                    questionTypeAndDifficultyDistributionValidationResultDto);
            }

            var paperMetadata = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetAll(o => o.Id == request.PaperId)
                .FirstOrDefaultAsync();

            if (paperMetadata == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    Resource.PaperNotFound,
                                    questionTypeAndDifficultyDistributionValidationResultDto);
            }

            var allQuestionIds = request.Sections
                .SelectMany(s => s.Distributions)
                .Where(d => d.QuestionIds != null && d.QuestionIds.Any())
                .SelectMany(d => d.QuestionIds)
                .ToList();

            var duplicates = allQuestionIds
                .GroupBy(q => q)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.DuplicateQuestionAcrossSections,
                    questionTypeAndDifficultyDistributionValidationResultDto
                );
            }

            long outputFormCount = paperMetadata.OutputFormsCount;

            var requiredQuestions = request
                .Sections
                .SelectMany(section => section.Distributions)
                .Where(dist => dist.QuestionIds == null || !dist.QuestionIds.Any())
                .GroupBy(dist => new
                {
                    dist.QuestionTypeID,
                    dist.DifficultyLevelID,
                    dist.ItemBankId,
                    SubQuestionCount = dist.QuestionTypeID == (long)QuestionType.Comprehension
                        ? dist.SubQuestionCount
                        : 0
                })
                .Select(group => new QuestionDistributionRequirementDto
                {
                    QuestionTypeID = group.Key.QuestionTypeID,
                    DifficultyLevelID = group.Key.DifficultyLevelID,
                    SubQuestionCount = group.Key.SubQuestionCount,
                    RequiredCount = group.Sum(d => d.SelectedCount) * outputFormCount
                })
                .ToList();

            foreach (var required in requiredQuestions)
            {
                var availableNode = flattenedNodes
                    .FirstOrDefault(node => node.QuestionTypeId == required.QuestionTypeID &&
                                            node.DifficultyLevelId == required.DifficultyLevelID &&
                                            (!node.AllowInstantResultPaper || node.IsAutoCorrectableQuestion) &&
                                            (required.QuestionTypeID != (long)QuestionType.Comprehension || node.SubQuestionCount == required.SubQuestionCount));

                if (availableNode == null || availableNode.QuestionCount < required.RequiredCount)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.DistributionFail,
                                        HttpStatusCode.BadRequest,
                                        Resource.DistributionFailedDueToNotEnoughQuestions,
                                        questionTypeAndDifficultyDistributionValidationResultDto);
                }
            }

            questionTypeAndDifficultyDistributionValidationResultDto.IsValid = true;

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.Distributioncompletedsuccessfully,
                                questionTypeAndDifficultyDistributionValidationResultDto);
        }

        private async Task<ApiResponse> ValidateManualQuestions(AddOrUpdateAutoSectionsDistributionsRequestDto request)
        {
            var manualQuestionIds = request.Sections
                .SelectMany(s => s.Distributions)
                .Where(d => d.QuestionIds != null && d.QuestionIds.Count > 0)
                .SelectMany(d => d.QuestionIds)
                .Distinct()
                .ToList();
            if (manualQuestionIds.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.Nomanualselectedquestionswerefound
                );
            }

            var response = await _questionService.GetAllQuestionsByIds(manualQuestionIds);
            if (response.CustomCodeStatus != CustomCodeStatus.Success || response.Data is not List<QuestionMetadataDto> questionsFromDb)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.Failedtofetchquestions
                );
            }

            var questionsDict = questionsFromDb.ToDictionary(q => q.Id);
            var missingId = manualQuestionIds.FirstOrDefault(id => !questionsDict.ContainsKey(id));
            if (missingId != default)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    $"{Resource.Nomanualselectedquestionswerefound}: {missingId}"
                );
            }

            foreach (var section in request.Sections)
            {
                foreach (var distribution in section.Distributions.Where(d => d.QuestionIds != null && d.QuestionIds.Count > 0))
                {
                    if (distribution.SelectedCount < distribution.QuestionIds.Count)
                    {
                        return _commonService._apiResponse.GetApiResponse(
                            CustomCodeStatus.Failure,
                            HttpStatusCode.BadRequest,
                            $"'{section.Name}': {Resource.SelectedCount} ({distribution.SelectedCount}) {Resource.doesnotmatchQuestionIdscount} ({distribution.QuestionIds.Count})."
                        );
                    }

                    var relatedQuestions = distribution.QuestionIds.Select(id => questionsDict[id]).ToList();

                    var wrongType = relatedQuestions.FirstOrDefault(q => q.QuestionTypeId != distribution.QuestionTypeID);
                    if (wrongType != null)
                    {
                        return _commonService._apiResponse.GetApiResponse(
                            CustomCodeStatus.Failure,
                            HttpStatusCode.BadRequest,
                            $"'{section.Name}': {Resource.Question} {wrongType.Id} {Resource.hastype} {wrongType.QuestionTypeId} {Resource.butdistributionrequires} {distribution.QuestionTypeID}."
                        );
                    }

                    var wrongDifficulty = relatedQuestions.FirstOrDefault(q => q.DifficultyLevelId != distribution.DifficultyLevelID);
                    if (wrongDifficulty != null)
                    {
                        return _commonService._apiResponse.GetApiResponse(
                            CustomCodeStatus.Failure,
                            HttpStatusCode.BadRequest,
                            $"'{section.Name}': {Resource.Question} {wrongDifficulty.Id} {Resource.hasdifficulty} {wrongDifficulty.DifficultyLevelId} {Resource.butdistributionrequires} {distribution.DifficultyLevelID}."
                        );
                    }

                    var exhausted = relatedQuestions.FirstOrDefault(q => q.CurrentExhaustionCount >= q.QuestionsExhaustionCount);
                    if (exhausted != null)
                    {
                        return _commonService._apiResponse.GetApiResponse(
                            CustomCodeStatus.Failure,
                            HttpStatusCode.BadRequest,
                            $"'{section.Name}': {Resource.Question} {exhausted.Id} {Resource.exceededitsexhaustionlimit}"
                        );
                    }
                }
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Manualquestionsvalidatedsuccessfully
            );
        }
    }
}
