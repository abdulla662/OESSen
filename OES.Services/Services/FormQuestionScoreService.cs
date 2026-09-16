using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.Dtos.SyncQuestions;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class FormQuestionScoreService(ICommonService _commonService, IFormService _formService) : IFormQuestionScoreService
    {
        // GET METHODS:

        public async Task<ApiResponse> GetQuestionsRelationsForManualPaperAsync(long paperId)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == paperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound,
                    null
                );
            }

            var itemBankPoints = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(x => x.PaperId == paperId,
                             Including:
                             $"{nameof(PaperItemBankPoint.Paper)}," +
                             $"{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)}," +
                             $"{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}")
                .ToListAsync();

            if (itemBankPoints.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoItemBankPointsFoundForPaper,
                    null
                );
            }

            var allManualQuestions = itemBankPoints
                .SelectMany(x => x.ManualPaperItemBankQuestionSections)
                .ToList();

            if (allManualQuestions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.Nomanualselectedquestionswerefound,
                    null
                );
            }

            var forms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAll(x => x.PaperId == paperId && !x.IsDeleted && x.IsActive)
                .ToListAsync();

            var formIds = forms.ConvertAll(f => f.Id);

            var formSections = await _commonService
                ._unitOfWork
                .Repository<StandardSection, long>()
                .GetAll(x => x.FormId.HasValue && formIds.Contains(x.FormId.Value))
                .ToListAsync();

            if (forms.Count > 0 && formSections.Count > 0)
            {
                var questionDtos = allManualQuestions
                     .ConvertAll((q) => new QuestionForPaperDto
                     {
                         PaperItemBankQuestionId = q.Id,
                         QuestionId = q.QuestionMetaDataId,
                         SectionId = q.SectionId,
                         FormId = formSections.Find(x => x.Id == q.SectionId)?.FormId ?? 0,
                         Score = 0
                     });

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.QuestionsFetchedSuccessfullyManualNoScores,
                    questionDtos
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.NotFound,
                HttpStatusCode.NotFound,
                Resource.NoFormsFoundForPaper,
                null
            );
        }


        // POST/PUT/DELETE METHODS:

        public async Task<ApiResponse> SaveFormsAndItsQuestions(long paperId, Dictionary<string, List<long>> formQuestionMap, FormMetadataDto formMetadata)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == paperId);

            if (paper is null)
            {
                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.NotFound,
                                   HttpStatusCode.NotFound,
                                   Resource.PaperNotFound);
            }

            return paper.QuestionSelectionType switch
            {
                QuestionSelectionType.Manual => await AssignManualQuestionsToFormsAsync(paperId, formQuestionMap, formMetadata),
                QuestionSelectionType.Auto => await AssignAutoQuestionsToFormsAsync(paperId),
                _ => _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidQuestionSelectionType
                )
            };
        }

        public async Task<ApiResponse> ApplyScoresToPaperFormsAsync(long paperId)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == paperId);

            if (paper is null)
            {
                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.NotFound,
                                   HttpStatusCode.NotFound,
                                   Resource.PaperNotFound);
            }

            return paper.QuestionSelectionType switch
            {
                QuestionSelectionType.Manual => await ApplyScoresForManualFormsAsync(paperId),
                QuestionSelectionType.Auto => await ApplyScoresForAutoFormsAsync(paperId),
                _ => _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidQuestionSelectionType
                )
            };
        }


        #region Helper Methods

        private async Task<ApiResponse> AssignManualQuestionsToFormsAsync(long paperId, Dictionary<string, List<long>> formQuestionMap, FormMetadataDto formMetadata)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == paperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound,
                    null
                );
            }

            var itemBankPoints = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(x => x.PaperId == paperId,
                             Including:
                             $"{nameof(PaperItemBankPoint.Paper)},"
                             + $"{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)},"
                             + $"{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}")
                .ToListAsync();

            if (itemBankPoints.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoItemBankPointsFoundForPaper,
                    null);
            }

            var allManualQuestions = itemBankPoints
                .SelectMany(x => x.ManualPaperItemBankQuestionSections)
                .ToList();

            if (allManualQuestions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoManualQuestionsFoundForPaper,
                    null
                );
            }

            var forms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAll(x => x.PaperId == paperId && !x.IsDeleted && x.IsActive)
                .ToListAsync();

            if (formMetadata.Id >= 0)
            {
                var formCreationResponse = await _formService.CreateFormsForManualPaperAsync(paperId, formMetadata);

                if (formCreationResponse.StatusCode == HttpStatusCode.OK)
                {
                    forms = formCreationResponse.Data as List<PaperForm>;
                }
                else
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.NotFound,
                        HttpStatusCode.NotFound,
                        formCreationResponse.Message
                    );
                }
            }

            var formsByName = forms.ToDictionary(f => f.Name.Trim(), f => f.Id);

            var formIdList = forms.ConvertAll(f => f.Id);

            var existingAssignments = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAllAsync(x => formIdList.Contains(x.FormId.Value));

            var existingFormIds = existingAssignments.Select(x => x.FormId.Value).Distinct().ToList();

            var formQuestionsDtoList = new List<FormQuestionsDto>();

            foreach (var entry in formQuestionMap)
            {
                var formName = entry.Key.Trim();
                var questionIdsForThisForm = entry.Value;

                if (formsByName.TryGetValue(formName, out var formId))
                {
                    if (existingFormIds.Contains(formId))
                        continue;

                    var questionsToAssign = allManualQuestions
                         .Where(q => questionIdsForThisForm.Contains(q.Id))
                         .ToList();

                    formQuestionsDtoList.Add(new FormQuestionsDto
                    {
                        FormId = formId,
                        Questions = questionsToAssign.ConvertAll(q => new QuestionWithScoreDto
                        {
                            QuestionId = q.QuestionMetaDataId,
                            Score = 0
                        })
                    });
                }
            }

            await _formService.AssignQuestionsToFormsAsync(formQuestionsDtoList);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsFetchedSuccessfullyManualNoScores
            );
        }

        private async Task<ApiResponse> AssignAutoQuestionsToFormsAsync(long paperId)
        {
            // 1. Validate paper
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == paperId);

            if (paper == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.PaperNotFound, null);

            // 2. Fetch item bank points
            var itemBankPoints = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .GetAll(x => x.PaperId == paperId,
                    Including:
                        $"{nameof(PaperItemBankPoint.Paper)}," +
                        $"{nameof(PaperItemBankPoint.ItemBank)}," +
                        $"{nameof(PaperItemBankPoint.AutoPaperItemBankQuestionSections)}")
                .ToListAsync();

            if (itemBankPoints.Count == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.Noitembankpointsfoundforthepaper, null);

            // 3. Fetch eligible questions
            var itemBankIds = itemBankPoints.Select(p => p.ItemBankId).Distinct().ToList();
            var autoSchemes = itemBankPoints.SelectMany(p => p.AutoPaperItemBankQuestionSections).ToList();

            var allMatchingQuestions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(q =>
                    itemBankIds.Contains(q.ItemBankId) &&
                    q.IsRoot &&
                    q.QuestionStatus == QuestionStatus.Approved &&
                    q.ParentId == null &&
                    q.QuestionDetails.Any(d => d.LanguageId == paper.LanguageId),
                    Including: nameof(QuestionMetadata.SubQuestions))
                .ToListAsync();

            // 4. Get or create forms
            var forms = await CreateThenGetFormsAsync(paperId, paper.OutputFormsCount);

            var formIdList = forms.OrderBy(f => f.Name).Select(f => f.Id).ToList();

            var existingAssignments = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAllAsync(x => formIdList.Contains(x.FormId.Value));

            var existingFormIds = existingAssignments.Select(x => x.FormId.Value).Distinct();

            var newFormIds = formIdList.Except(existingFormIds).ToList();

            // 5. Build and distribute questions
            var result = BuildQuestionAssignments(paper, autoSchemes, allMatchingQuestions, newFormIds);

            // 6. Validate distribution
            var validationError = ValidateQuestionDistribution(autoSchemes, result);
            if (validationError != null)
                return validationError;

            // 7. Persist assignments
            await PersistAssignmentsAsync(result);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.QuestionsfetchedsuccessfullyAutoManualmergedscoresnotapplied, result);
        }

        private async Task<ApiResponse> ApplyScoresForManualFormsAsync(long paperId)
        {
            // 1) Get the questions:
            var formQuestionRecords = await GetQuestionsForManualPaperAsync(paperId);

            var formQuestionsGroups = formQuestionRecords
                .GroupBy(q => q.FormId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 2) Get the scoring scheme:
            var markingScheme = await _commonService
                ._unitOfWork
                .Repository<MarkingScheme, long>()
                .GetObjAsync(x => x.PaperId == paperId);

            if (markingScheme is null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScoringSchemeNotFound
                );
            }

            // 3) Apply scores based on the scoring scheme:
            switch (markingScheme.ScoreType)
            {
                case ScoreSchemaType.EqualDistribution:
                    {
                        var schemeData = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<EqualDistributionForManualDto>>>(markingScheme.Data);

                        var schemeLookup = schemeData.Data.ToDictionary(d => (d.FormId, d.QuestionMetadataId), d => d.Mark);

                        foreach (var formQuestion in formQuestionsGroups.Values.SelectMany(q => q))
                        {
                            if (schemeLookup.TryGetValue((formQuestion.FormId, formQuestion.QuestionId ?? 0), out var mark))
                            {
                                formQuestion.Score = mark;
                            }
                        }
                        break;
                    }

                case ScoreSchemaType.DifficultyLevelBasedDistribution:
                    {
                        var schemeData = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<DifficultyLevelDistributionDto>>>(markingScheme.Data);

                        var schemeLookup = schemeData.Data.ToDictionary(d => (d.FormId, d.DifficultyLevelId), d => d.MarkPerQuestion);

                        foreach (var formQuestion in formQuestionsGroups.Values.SelectMany(q => q))
                        {
                            if (schemeLookup.TryGetValue((formQuestion.FormId, formQuestion.Question.DifficultyLevelId), out var markPerQuestion))
                            {
                                formQuestion.Score = markPerQuestion * GetUnits(formQuestion.Question);
                            }
                        }
                        break;
                    }

                case ScoreSchemaType.ItemBankBasedDistribution:
                    {
                        var schemeData = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<ItemBankDistributionDto>>>(markingScheme.Data);

                        var schemeLookup = schemeData.Data.ToDictionary(d => (d.FormId, d.ItemBankId), d => d.QuestionMark);

                        foreach (var formQuestion in formQuestionsGroups.Values.SelectMany(q => q))
                        {
                            if (schemeLookup.TryGetValue((formQuestion.FormId, formQuestion.Question.ItemBankId), out var markPerQuestion))
                            {
                                formQuestion.Score = markPerQuestion * GetUnits(formQuestion.Question);
                            }
                        }
                        break;
                    }
            }

            // 4) Update FormQuestions entities in the database, since they are tracked by default:

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.ScoresAppliedSuccessfullyForAutoPaper
            );
        }

        private async Task<ApiResponse> ApplyScoresForAutoFormsAsync(long paperId)
        {
            var paper = await _commonService
               ._unitOfWork
               .Repository<PaperMetadata, long>()
               .GetObjAsync(p => p.Id == paperId);

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound,
                    null
                );
            }

            var scoringScheme = await _commonService
                ._unitOfWork
                .Repository<MarkingScheme, long>()
                .GetObjAsync(s => s.PaperId == paperId);

            if (scoringScheme == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.ScoringSchemeNotFound,
                    null
                );
            }

            /*
                 NOTE:
                 Previously, scores were applied to all questions across all forms associated with the paper.
                 However, auto forms are generated incrementally, and only the last generated form is active during editing.

                 Updating scores for all forms would unintentionally overwrite scores of previously finalized forms.

                 To prevent this, scores must be applied only to questions belonging to the last generated form.
                 Therefore, we first retrieve all form questions associated with the paper to identify the available forms,
                 then determine the most recently created form, and apply score updates only to its questions,
                 excluding all other forms to preserve their finalized state and scores.
            */

            var paperForms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAllAsync(f => f.PaperId == paperId && !f.IsDeleted && f.IsActive);

            var lastFormId = paperForms
                .OrderByDescending(f => f.Id) // last created form
                .Select(f => f.Id)
                .FirstOrDefault();

            if (lastFormId == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.FormNotFound,
                    null
                );
            }

            var lastFormQuestions = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAllAsync(fq => fq.FormId == lastFormId);

            if (!lastFormQuestions.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoQuestionsAssignedToFormsForPaper,
                    null
                );
            }

            var questionIds = lastFormQuestions
                .Where(q => q.QuestionId.HasValue)
                .Select(q => q.QuestionId.Value)
                .ToList();

            var questions = await _commonService._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(q => questionIds.Contains(q.Id));

            var questionDict = questions.ToDictionary(q => q.Id, q => q);

            switch (scoringScheme.ScoreType)
            {
                case ScoreSchemaType.EqualDistribution:
                    {
                        var json = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<EqualDistributionForAutoDto>>>(scoringScheme.Data);

                        if (json?.Data == null || json.Data.Count == 0)
                        {
                            break;
                        }

                        var scoresList = json.Data.ConvertAll(d => d.Mark);

                        int numberOfScores = scoresList.Count;

                        var lastFormQuestionsList = lastFormQuestions.ToList();

                        for (int i = 0; i < lastFormQuestionsList.Count; i++)
                        {
                            var currentFormQuestion = lastFormQuestionsList[i];

                            int scoreIndex = i % numberOfScores;

                            currentFormQuestion.Score = scoresList[scoreIndex];
                        }

                        break;
                    }

                case ScoreSchemaType.DifficultyLevelBasedDistribution:
                    {
                        var json = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<DifficultyLevelDistributionDto>>>(scoringScheme.Data);

                        foreach (var q in lastFormQuestions)
                        {
                            var question = questionDict[q.QuestionId.Value];

                            var scoreEntry = json?.Data.FirstOrDefault(d => d.DifficultyLevelId == question?.DifficultyLevelId);

                            q.Score = scoreEntry?.MarkPerQuestion ?? 0;
                        }

                        break;
                    }

                case ScoreSchemaType.ItemBankBasedDistribution:
                    {
                        var json = JsonSerializer.Deserialize<GenericMarkingSchemeApplicationDto<List<ItemBankDistributionDto>>>(scoringScheme.Data);

                        foreach (var q in lastFormQuestions)
                        {
                            var question = questionDict[q.QuestionId.Value];

                            var scoreEntry = json?.Data.FirstOrDefault(d => d.ItemBankId == question.ItemBankId);

                            q.Score = scoreEntry?.QuestionMark ?? 0;
                        }

                        break;
                    }
            }

            _commonService._unitOfWork.Repository<GeneratedFormQuestion, long>().UpdateRange([.. lastFormQuestions]);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.ScoresAppliedSuccessfullyForAutoPaper,
                lastFormQuestions
            );
        }

        private async Task<List<GeneratedFormQuestion>> GetQuestionsForManualPaperAsync(long paperId)
        {
            var formQuestionRecords = await _commonService
                 ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAllAsync(
                    x => x.Form.PaperId == paperId,
                    Including: $"{nameof(GeneratedFormQuestion.Form)}," +
                    $"{nameof(GeneratedFormQuestion.Question)}," +
                    $"{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.SubQuestions)}"
                );

            return [.. formQuestionRecords];
        }

        #endregion Helper Methods

        #region Helper Methods of Helper Methods

        private async Task<List<PaperForm>> CreateThenGetFormsAsync(long paperId, long paperFinalOutputFormsCount)
        {
            var forms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAll(x => x.PaperId == paperId && !x.IsDeleted)
                .ToListAsync();

            var existingMaxNumber = forms
                .Select(f =>
                {
                    var lastPart = f.Name?.Split(' ').LastOrDefault();
                    return int.TryParse(lastPart, out var num) ? num : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            var formsNeeded = paperFinalOutputFormsCount - forms.Count;

            var startingCounter = existingMaxNumber + 1;

            var adjustedUpperBound = existingMaxNumber + formsNeeded;

            var formCreationResponse = await _formService.CreateFormsForAutoPaperAsync(paperId, startingCounter, adjustedUpperBound);

            if (formCreationResponse.StatusCode == HttpStatusCode.OK)
                forms.AddRange(formCreationResponse.Data as List<PaperForm>);

            return forms ?? [];
        }

        private static List<QuestionForPaperDto> BuildQuestionAssignments(
            PaperMetadata paper,
            List<AutoPaperItemBankQuestionSection> autoSchemes,
            List<QuestionMetadata> allMatchingQuestions,
            List<long> newFormIdsList
        )
        {
            var result = new List<QuestionForPaperDto>();
            var newFormsCount = newFormIdsList.Count;

            foreach (var scheme in autoSchemes)
            {
                var manualQuestionIds = JsonSerializer.Deserialize<List<long>>(scheme.QuestionIds ?? "[]") ?? [];

                var manualQuestions = allMatchingQuestions
                    .Where(q => manualQuestionIds.Contains(q.Id))
                    .ToList();

                // Calculate how many AUTO questions we need PER FORM
                int manualCount = manualQuestions.Count;

                int autoCountPerForm = (int)scheme.SelectedCount - manualCount;

                // STEP 1: Assign manual questions to form first
                foreach (var formId in newFormIdsList)
                {
                    foreach (var manualQuestion in manualQuestions)
                    {
                        result.Add(new QuestionForPaperDto
                        {
                            QuestionId = manualQuestion.Id,
                            SectionId = scheme.SectionId,
                            FormId = formId,
                            ItemBankId = scheme.ItemBankPoint.ItemBankId,
                            Score = 0
                        });
                    }
                }

                // STEP 2: Build pool for AUTO questions
                if (autoCountPerForm > 0)
                {
                    int totalAutoNeeded = 0;

                    if (paper.QuestionDistributionTypeInForm == QuestionDistributionTypeInForm.TotallyDistinct)
                        totalAutoNeeded = autoCountPerForm * newFormsCount;

                    else if (paper.QuestionDistributionTypeInForm == QuestionDistributionTypeInForm.PartiallyDistinct)
                        totalAutoNeeded = autoCountPerForm + 1;

                    // Build auto question pool (excluding manual questions)
                    var autoPool = allMatchingQuestions
                        .Where(q =>
                            q.DifficultyLevelId == scheme.DifficultyLevelID &&
                            q.ItemBankId == scheme.ItemBankPoint.ItemBankId &&
                            q.QuestionTypeId == scheme.QuestionTypeID &&
                            q.CurrentExhaustionCount < q.QuestionsExhaustionCount &&
                            !manualQuestionIds.Contains(q.Id) &&
                            (
                                scheme.SubQuestionCount <= 1
                                    ? q.SubQuestions.Count == 0 // Normal questions
                                    : q.SubQuestions.Count == scheme.SubQuestionCount // Comprehension questions
                            ))
                        .OrderBy(_ => Guid.NewGuid())
                        .Take(totalAutoNeeded)
                        .ToList();

                    if (paper.QuestionDistributionTypeInForm == QuestionDistributionTypeInForm.PartiallyDistinct)
                    {
                        if (autoPool.Count < totalAutoNeeded && autoPool.Count > 0)
                        {
                            var duplicated = new List<QuestionMetadata>();

                            while (duplicated.Count < totalAutoNeeded)
                                duplicated.AddRange(autoPool);

                            autoPool = duplicated.Take(totalAutoNeeded).ToList();
                        }
                    }

                    // STEP 3: Distribute AUTO questions to forms
                    if (paper.QuestionDistributionTypeInForm == QuestionDistributionTypeInForm.TotallyDistinct)
                    {
                        for (int formIndex = 0; formIndex < newFormsCount; formIndex++)
                        {
                            var formId = newFormIdsList[formIndex];

                            int startIndex = formIndex * autoCountPerForm;

                            for (int i = 0; i < autoCountPerForm; i++)
                            {
                                int idx = startIndex + i;

                                if (idx >= autoPool.Count) break;

                                result.Add(new QuestionForPaperDto
                                {
                                    QuestionId = autoPool[idx].Id,
                                    SectionId = scheme.SectionId,
                                    FormId = formId,
                                    ItemBankId = scheme.ItemBankPoint.ItemBankId,
                                    Score = 0
                                });
                            }
                        }
                    }
                    else if (paper.QuestionDistributionTypeInForm == QuestionDistributionTypeInForm.PartiallyDistinct)
                    {
                        for (int formIndex = 0; formIndex < newFormsCount; formIndex++)
                        {
                            var formId = newFormIdsList[formIndex];

                            for (int i = 0; i < autoCountPerForm; i++)
                            {
                                int poolIndex = (formIndex * autoCountPerForm + i) % autoPool.Count;

                                result.Add(new QuestionForPaperDto
                                {
                                    QuestionId = autoPool[poolIndex].Id,
                                    SectionId = scheme.SectionId,
                                    FormId = formId,
                                    ItemBankId = scheme.ItemBankPoint.ItemBankId,
                                    Score = 0
                                });
                            }
                        }
                    }
                }
            }

            return result;
        }

        private ApiResponse ValidateQuestionDistribution(List<AutoPaperItemBankQuestionSection> autoSchemes, List<QuestionForPaperDto> result)
        {
            var totalQuestionsExpectedPerForm = autoSchemes.Sum(s => s.SelectedCount);

            var actualQuestionsPerForm = result
                .GroupBy(r => r.FormId)
                .ToDictionary(g => g.Key, g => g.Count());

            if (actualQuestionsPerForm.Count != 0 && actualQuestionsPerForm.Values.Any(count => count != totalQuestionsExpectedPerForm))
            {
                var errorDetails = string.Join(", ", actualQuestionsPerForm.Select(kvp =>
                    $"{Resource.Form} {kvp.Key} {Resource.has} {kvp.Value} {Resource.Questions}"));

                var errorMessage = $"{Resource.FormvalidationfailedThenumberofquestionsperformisinconsistentExpected} {totalQuestionsExpectedPerForm} {Resource.Questions} {Resource.forallforms}: {errorDetails}";

                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure, HttpStatusCode.BadRequest, errorMessage, null);
            }

            return null;
        }

        private async Task PersistAssignmentsAsync(List<QuestionForPaperDto> result)
        {
            var formQuestionsDtoList = result
                .GroupBy(r => r.FormId)
                .Select(g => new FormQuestionsDto
                {
                    FormId = g.Key,
                    Questions = [.. g.Select(q => new QuestionWithScoreDto
                    {
                        QuestionId = q.QuestionId,
                        PaperQuestionStatus = q.PaperQuestionStatus,
                        Score = q.Score
                    })]
                })
                .ToList();

            var questionIds = formQuestionsDtoList
                .SelectMany(fq => fq.Questions)
                .Select(q => q.QuestionId)
                .Distinct()
                .ToList();

            await _formService.AssignQuestionsToFormsAsync(formQuestionsDtoList);
        }

        private static int GetUnits(QuestionMetadata q)
        {
            if (q.QuestionTypeId != (long)Helper.Enums.QuestionType.Comprehension)
            {
                return 1;
            }

            return q.SubQuestions?.Count > 0 ? q.SubQuestions.Count : 1;
        }

        #endregion Helper Methods of Helper Methods
    }
}