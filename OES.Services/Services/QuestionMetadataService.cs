using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.QuestionVersions;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Data;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class QuestionMetadataService(ICommonService _commonService, IMapper _mapper, IQuestionHtmlHelperService _htmlHelperService, FilterParamsValues _filterParamsValues, QuestionDeltaFileProcessingService _questionDeltaFileProcessingService) : IQuestionMetadataService
    {
        public async Task<IApiResponse> GetPaginatedSubQuestions(long parentId, PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(result => result.ParentId == parentId, null, $"{nameof(QuestionDetails)}.{nameof(QuestionDetails.Language)},{nameof(QuestionDetails)}.{nameof(QuestionDetails.QuestionsChoices)}")
                .AsNoTracking();

            if (!string.IsNullOrEmpty(pagination.SearchKey))
            {
                query = query.Where(q => q.Code.Contains(pagination.SearchKey));
            }

            var totalItemsCount = await query.CountAsync();

            if (!pagination.PaginationOff)
            {
                query = query
                    .Skip(pagination.PageIndex * pagination.PageSize)
                    .Take(pagination.PageSize);
            }

            var subQuestionsMetadata = await query.ToListAsync();

            var subQuestionsDtos = subQuestionsMetadata.ConvertAll(qm =>
            {
                var subQuestionDetailsGroup = qm.QuestionDetails ?? Enumerable.Empty<QuestionDetails>();
                var subQuestionDetailsGroupChoicesCountsList = qm.QuestionDetails.Select(x => x.QuestionsChoices.Count).ToList();
                var firstSubQuestionDetails = subQuestionDetailsGroup.FirstOrDefault();
                var subQuestionLanguages = subQuestionDetailsGroup.DistinctBy(sqd => sqd.LanguageId).Select(sqd => sqd.Language);

                return new PaginatedListSubQuestionDto
                {
                    QuestionMetadataId = qm.Id,
                    Body = firstSubQuestionDetails?.Body ?? "No Body",
                    ChoicesCountsList = subQuestionDetailsGroupChoicesCountsList,
                    NumberOfLanguages = subQuestionLanguages.Count(),
                    LanguagesIds = [.. subQuestionLanguages.Select(sql => sql.Id)],
                    LanguageNames = string.Join(", ", subQuestionLanguages.Select(sql => sql.Name)),
                    Delta = qm.Delta.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)
                };
            });

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.SubQuestionsFetchedSuccessfully,
                                new CustomTableData<PaginatedListSubQuestionDto>(subQuestionsDtos, totalItemsCount));

        }

        public async Task<IApiResponse> GetQuestionMetaDataForQCView(long questionId)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(x => x.Id == questionId)
                .Include(x => x.QuestionDetails).ThenInclude(qd => qd.Language)
                .Include(x => x.QuestionDetails).ThenInclude(qd => qd.QuestionsChoices)
                .Include(x => x.QuestionDetails).ThenInclude(qd => qd.SegmentQuestionProperties)
                .Include(x => x.DifficultyProfile)
                .Include(x => x.Ilo)
                .Include(x => x.ItemBank)
                .Include(x => x.Subject)
                .Include(x => x.QuestionType)
                .Include(x => x.QuestionCategory)
                .Include(x => x.DifficultyLevel)
                .Include(x => x.FileUploadResponseSettings)
                .Include(x => x.QLayout)
                .FirstOrDefault();

            if (query == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.FailedLoadExamQuestionDetails);
            }

            var data = _mapper.Map<QuestionMetadata, GetQuestionMetaDataForQCViewDto>(query);

            if (query.FileUploadResponseSettings != null)
            {
                data.FileUploadSettings = new FileUploadSettingsDto
                {
                    Id = query.FileUploadResponseSettings.Id,
                    QuestionMetadataId = query.FileUploadResponseSettings.QuestionMetadataId,
                    ShowAnswerTextArea = query.FileUploadResponseSettings.ShowAnswerTextArea,
                    SupportedFileExtensions = query.FileUploadResponseSettings.SupportedFileExtensions,
                    UploadedFilesCount = query.FileUploadResponseSettings.UploadedFilesCount,
                    SingleFileMaxSizeInMB = query.FileUploadResponseSettings.SingleFileMaxSizeInMB
                };
            }

            foreach (var questionDetail in data.QuestionDetails)
            {
                if (questionDetail.HasShuffled)
                {
                    questionDetail.QuestionsChoices.Shuffle();
                }
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.SubQuestionsFetchedSuccessfully,
                                data);
        }

        public async Task<IApiResponse> CheckIfQuestionMetadataHasAnyQuestionDetailsAsync(long questionMetadataId)
        {
            var questionDetailsCount = await _commonService
                ._unitOfWork
                .Repository<QuestionDetails, long>()
                .Query()
                .LongCountAsync(qd => qd.QuestionMetadataId == questionMetadataId);

            var questionMetadataCheckResultDto = new QuestionMetadataCheckResultDto();

            if (questionDetailsCount > 0)
            {
                questionMetadataCheckResultDto.HasAnyQuestionDetails = true;

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null,
                                    questionMetadataCheckResultDto);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    null,
                                    questionMetadataCheckResultDto);
        }

        public async Task<IApiResponse> CopyOldQuestionDeeplyAsync(long questionMetadataId)
        {
            var oldQuestion = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => x.Id == questionMetadataId,
                    Including: "QuestionDetails," +
                               "QuestionDetails.QuestionsChoices," +
                               "QuestionDetails.SegmentQuestionProperties," +
                               "QuestionDetails.MatchingPairQuestionItems," +
                               "FileUploadResponseSettings"
                )
                .AsNoTracking()
                .FirstOrDefault();

            if (oldQuestion != null)
            {
                var subQuestionTypes = new List<long>
                {
                    (long)Helper.Enums.QuestionType.Comprehension,
                    (long)Helper.Enums.QuestionType.MatchingPairs,
                    (long)Helper.Enums.QuestionType.Segment,
                    (long)Helper.Enums.QuestionType.MatchingPairsWithDragDrop
                };

                var copiedQuestion = _mapper.Map<QuestionMetadata>(oldQuestion);

                copiedQuestion.CurrentExhaustionCount = 0;

                List<QuestionMetadata> copiedSubQuestionsList = [];

                await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .AddAsync(copiedQuestion);

                await _commonService._unitOfWork.Complete();

                var choiceIdMap = BuildChoiceIdMap(oldQuestion, copiedQuestion);
                RemapModelAnswerChoiceIds(copiedQuestion.QuestionDetails, choiceIdMap);

                if (oldQuestion.QuestionTypeId == (long)Helper.Enums.QuestionType.MatchingPairsWithDragDrop ||
                    oldQuestion.QuestionTypeId == (long)Helper.Enums.QuestionType.MatchingPairs)
                {
                    UpdateMatchingPairsWithDragDropModelAnswer(oldQuestion, copiedQuestion);

                    _commonService._unitOfWork.Repository<QuestionDetails, long>().UpdateRange(copiedQuestion.QuestionDetails.ToList());

                    await _commonService._unitOfWork.Complete();
                }

                if (subQuestionTypes.Contains(oldQuestion.QuestionTypeId))
                {
                    copiedSubQuestionsList = CopySubQuestions(questionMetadataId, copiedQuestion.Id);
                }

                if (copiedSubQuestionsList.Count > 0)
                {
                    _commonService
                        ._unitOfWork
                        .Repository<QuestionMetadata, long>()
                        .AddRangAsync(copiedSubQuestionsList);
                }

                var oldGroups = await _commonService
                    ._unitOfWork
                    .Repository<QuestionGroups, long>()
                    .GetAll(qg => qg.QuestionId == questionMetadataId)
                    .AsNoTracking()
                    .ToListAsync();

                if (oldGroups.Count > 0)
                {
                    var newGroups = oldGroups.ConvertAll(g => new QuestionGroups
                    {
                        QuestionId = copiedQuestion.Id,
                        OESGroupId = g.OESGroupId
                    });

                    _commonService
                        ._unitOfWork
                        .Repository<QuestionGroups, long>()
                        .AddRangAsync(newGroups);
                }

                await _commonService._unitOfWork.Complete();

                var allCopiedDetails = copiedQuestion.QuestionDetails?.ToList() ?? [];

                foreach (var subQuestion in copiedSubQuestionsList ?? [])
                {
                    if (subQuestion.QuestionDetails != null)
                        allCopiedDetails.AddRange(subQuestion.QuestionDetails);
                }

                if (allCopiedDetails.Count > 0)
                {
                    var extractedFiles = _htmlHelperService.ExtractMediaEntitiesFromQuestionDetails(allCopiedDetails);

                    if (extractedFiles.Count > 0)
                    {
                        _commonService
                            ._unitOfWork
                            .Repository<QuestionDocLibFile, long>()
                            .AddRangAsync(extractedFiles);

                        await _commonService._unitOfWork.Complete();
                    }
                }

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.Questioncopiedsuccessfully);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.InternalServerError,
                                HttpStatusCode.InternalServerError,
                                Resource.QuestionNotFound);
        }

        public async Task<ApiResponse> IsTheDocumentUsedInAnyQuestionAsync(Guid documentId)
        {
            var questionsUseThisDocument = await _commonService
                ._unitOfWork
                .Repository<QuestionDocLibFile, long>()
                .GetAllAsync(
                    q => q.FileId == documentId && !q.QuestionMetadata.IsDeleted,
                    Including: nameof(QuestionDocLibFile.QuestionMetadata)
                );

            if (questionsUseThisDocument.Any())
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK);
            }

            return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotAllowed, HttpStatusCode.BadRequest);
        }

        private List<QuestionMetadata> CopySubQuestions(long oldParentId, long newParentId)
        {
            List<QuestionMetadata> copiedSubQuestionsList = [];

            var subQuestions = _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAll(x => x.ParentId == oldParentId, Including: "QuestionDetails.QuestionsChoices,QuestionDetails.SegmentQuestionProperties,QuestionDetails.MatchingPairQuestionItems")
                .AsNoTracking();

            foreach (var subQuestion in subQuestions)
            {
                var copiedSubQuestion = _mapper.Map<QuestionMetadata>(subQuestion);

                copiedSubQuestion.ParentId = newParentId;

                copiedSubQuestion.CurrentExhaustionCount = 0;

                copiedSubQuestion.Delta = subQuestion.Delta;

                copiedSubQuestionsList.Add(copiedSubQuestion);
            }

            return copiedSubQuestionsList;
        }

        private void UpdateMatchingPairsWithDragDropModelAnswer(QuestionMetadata oldQuestion, QuestionMetadata copiedQuestion)
        {
            var idMapping = new Dictionary<long, long>();

            var oldDetails = oldQuestion.QuestionDetails.OrderBy(d => d.LanguageId).ToList();
            var newDetails = copiedQuestion.QuestionDetails.OrderBy(d => d.LanguageId).ToList();

            for (int i = 0; i < oldDetails.Count && i < newDetails.Count; i++)
            {
                var oldItems = oldDetails[i].MatchingPairQuestionItems.OrderBy(item => item.ColumnOrder).ThenBy(item => item.Id).ToList();
                var newItems = newDetails[i].MatchingPairQuestionItems.OrderBy(item => item.ColumnOrder).ThenBy(item => item.Id).ToList();

                for (int j = 0; j < oldItems.Count && j < newItems.Count; j++)
                {
                    idMapping[oldItems[j].Id] = newItems[j].Id;
                }
            }

            foreach (var detail in newDetails)
            {
                if (string.IsNullOrWhiteSpace(detail.ModelAnswer)) continue;
                var oldModelAnswer = System.Text.Json.JsonSerializer.Deserialize<List<MatchingPairModelAnswerDto>>(detail.ModelAnswer);
                if (oldModelAnswer == null) continue;

                var newModelAnswer = new List<MatchingPairModelAnswerDto>();
                foreach (var item in oldModelAnswer)
                {
                    if (idMapping.TryGetValue(item.QuestionItemId, out var newQuestionId))
                    {
                        var newAnswerIds = new List<long>();
                        foreach (var oldAnswerId in item.AnswerIds)
                        {
                            if (idMapping.TryGetValue(oldAnswerId, out var newAnswerId))
                            {
                                newAnswerIds.Add(newAnswerId);
                            }
                        }

                        if (newAnswerIds.Count > 0)
                        {
                            newModelAnswer.Add(new MatchingPairModelAnswerDto
                            {
                                QuestionItemId = newQuestionId,
                                AnswerIds = newAnswerIds
                            });
                        }
                    }
                }
                detail.ModelAnswer = System.Text.Json.JsonSerializer.Serialize(newModelAnswer);
            }
        }

        public async Task<IApiResponse> GetQuestionVersionsAsync(long questionMetadataId)
        {
            var questionMetadata = await _commonService
               ._unitOfWork
               .Repository<QuestionMetadata, long>()
               .GetAll(qm => qm.Id == questionMetadataId)
               .AsNoTracking()
               .Select(qm => new { qm.Id, qm.QuestionTypeId })
               .FirstOrDefaultAsync();

            if (questionMetadata == null)
                return EmptyVersionsResponse();

            long questionTypeId = questionMetadata.QuestionTypeId;
            bool isComprehension = questionTypeId == (long)Helper.Enums.QuestionType.Comprehension;
            bool isSegment = questionTypeId == (long)Helper.Enums.QuestionType.Segment;

            var versions = await _commonService
               ._unitOfWork
               .Repository<QuestionDetailsVersions, long>()
               .GetAll(v => v.QuestionMetadataId == questionMetadataId)
               .OrderBy(v => v.LanguageId)
               .ThenBy(v => v.VersionNumber)
               .Include(v => v.SegmentQuestionPropertiesVersions)
               .AsNoTracking()
               .ToListAsync();

            if (versions.Count == 0)
                return EmptyVersionsResponse();

            var questionDetailIds = versions.Select(v => v.QuestionDetailsId).ToHashSet();
            var languageIds = versions.Select(v => v.LanguageId).ToHashSet();

            List<QuestionDetailsVersions> subVersions = [];
            Dictionary<long, long> subMetadataTypes = [];

            if (isComprehension || isSegment)
            {
                var subMetadataList = await _commonService
                    ._unitOfWork
                    .Repository<QuestionMetadata, long>()
                    .GetAll(qm => qm.ParentId == questionMetadataId)
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Select(qm => new { qm.Id, qm.QuestionTypeId })
                    .ToListAsync();

                if (subMetadataList.Count > 0)
                {
                    var subMetadataIds = subMetadataList.ConvertAll(x => x.Id);

                    subMetadataTypes = subMetadataList
                        .ToDictionary(x => x.Id, x => (long)x.QuestionTypeId);

                    var subVersionQuery = _commonService
                        ._unitOfWork
                        .Repository<QuestionDetailsVersions, long>()
                        .GetAll(v => subMetadataIds.Contains(v.QuestionMetadataId))
                        .OrderBy(v => v.QuestionMetadataId)
                        .ThenBy(v => v.VersionNumber)
                        .AsNoTracking();

                    // For segment sub-questions, also load their segment properties versions
                    if (isSegment)
                    {
                        subVersionQuery = subVersionQuery.Include(v => v.SegmentQuestionPropertiesVersions);
                    }

                    subVersions = await subVersionQuery.ToListAsync();

                    foreach (var sv in subVersions)
                    {
                        questionDetailIds.Add(sv.QuestionDetailsId);
                        languageIds.Add(sv.LanguageId);
                    }
                }
            }

            var languages = await _commonService
               ._unitOfWork
               .Repository<Language, long>()
               .GetAll(l => languageIds.Contains(l.Id))
               .AsNoTracking()
               .ToDictionaryAsync(l => l.Id, l => l.Name);

            var allChoices = await _commonService
                ._unitOfWork
                .Repository<QuestionsChoicesVersions, long>()
                .GetAll(c => questionDetailIds.Contains(c.QuestionDetailsId))
                .AsNoTracking()
                .ToListAsync();

            var choicesByKey = allChoices
               .GroupBy(c => (c.QuestionDetailsId, c.VersionNumber))
               .ToDictionary(g => g.Key, g => g.ToList());

            var allMatchingPairs = await _commonService
                ._unitOfWork
                .Repository<MatchingPairQuestionItemsVersions, long>()
                .GetAll(m => questionDetailIds.Contains(m.QuestionDetailsId))
                .AsNoTracking()
                .ToListAsync();

            var matchingPairsByKey = allMatchingPairs
               .GroupBy(m => (m.QuestionDetailsId, m.VersionNumber))
               .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ColumnOrder).ThenBy(x => x.Id).ToList());

            var subQuestionVersionsMap = new Dictionary<long, List<QuestionDetailsVersionDto>>();

            foreach (var sv in subVersions)
            {
                long subTypeId = subMetadataTypes.TryGetValue(sv.QuestionMetadataId, out var t) ? t : 0;

                var subDto = MapVersionToDto(sv, subTypeId, languages, choicesByKey, matchingPairsByKey);

                if (!subQuestionVersionsMap.TryGetValue(sv.VersionNumber, out var list))
                    subQuestionVersionsMap[sv.VersionNumber] = list = [];

                list.Add(subDto);
            }

            var result = versions.ConvertAll(v =>
            {
                var dto = MapVersionToDto(v, questionTypeId, languages, choicesByKey, matchingPairsByKey);
                dto.SubQuestions = subQuestionVersionsMap.TryGetValue(v.VersionNumber, out var subs)
                    ? subs
                    : [];
                return dto;
            });

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Empty,
                result
            );
        }

        public async Task<IApiResponse> ValidateQuestionDeltaFromExcelAsync(UpdateQuestionDeltaRequestDto request)
        {
            if (request.File is null || request.File.Length == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.NoFileUploaded
                );
            }

            var processed = await _questionDeltaFileProcessingService.ProcessFileAsync(request.File);

            if (processed.StatusCode != HttpStatusCode.OK)
            {
                var excelErrors = (processed.Data as List<QuestionDeltaExcelValidationErrorDto>) ?? [];

                var rows = excelErrors.ConvertAll(e => new QuestionDeltaRowResultDto(
                    RowNumber: e.RowNumber ?? 0,
                    QuestionCode: e.QuestionCode ?? string.Empty,
                    DeltaValue: e.DeltaValue,
                    Status: QuestionDeltaRowStatus.Error,
                    Message: $"{e.FieldName}: {e.ErrorMessage}"
                ));

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    processed.Message,
                    new QuestionDeltaValidationResponseDto(
                        CanProceed: false,
                        HasWarnings: false,
                        Rows: rows
                    )
                );
            }

            var dtos = (List<UpdateQuestionDeltaDto>)processed.Data ?? [];

            return await ValidateQuestionDeltaAsync(dtos);
        }

        public async Task<IApiResponse> UpdateQuestionDeltaAsync(UpdateQuestionDeltaConfirmDto updateQuestionDeltaConfirmDto)
        {
            if (updateQuestionDeltaConfirmDto.UpdateQuestionDeltas is null || updateQuestionDeltaConfirmDto.UpdateQuestionDeltas.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.NoData
                );
            }

            var deltaDictionary = updateQuestionDeltaConfirmDto.UpdateQuestionDeltas.ToDictionary(x => x.QuestionCode);

            var questions = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(q => deltaDictionary.Keys.Contains(q.Code))
                .ToListAsync();

            foreach (var question in questions)
            {
                if (deltaDictionary.TryGetValue(question.Code, out var row))
                {
                    question.Delta = row.DeltaValue!.Value;
                    question.LastDeltaUpdatedFromExcel = DateTimeHelper.Now;
                    question.DeltaUpdatedFromExcelBy = _filterParamsValues.UserEmail;
                }
            }

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionUpdatedSuccessfully
            );
        }

        private static QuestionDetailsVersionDto MapVersionToDto(
            QuestionDetailsVersions v,
            long questionTypeId,
            Dictionary<long, string> languages,
            Dictionary<(long QuestionDetailsId, long VersionNumber), List<QuestionsChoicesVersions>> choicesByKey,
            Dictionary<(long QuestionDetailsId, long VersionNumber), List<MatchingPairQuestionItemsVersions>> matchingPairsByKey = null)
        {
            return new QuestionDetailsVersionDto
            {
                QuestionMetadataId = v.QuestionMetadataId,
                VersionNumber = v.VersionNumber,
                QuestionTypeId = questionTypeId,
                LanguageName = languages.TryGetValue(v.LanguageId, out var lang)
                    ? lang
                    : v.LanguageId.ToString(),
                LanguageId = v.LanguageId,
                Body = v.Body,
                Instructions = v.Instructions,
                ModelAnswer = v.ModelAnswer,
                HasShuffled = v.HasShuffled,
                MaxWords = v.MaxWords,
                MaxRecordingTimeInSeconds = v.MaxRecordingTimeInSeconds,
                UseArabicNumbers = v.UseArabicNumbers,
                AttachmentFileName = v.AttachmentFileName,
                CreationDate = v.CreationDate,
                CreatedBy = v.CreationUser,
                Choices = choicesByKey.TryGetValue((v.QuestionDetailsId, v.VersionNumber), out var choices)
                    ? choices.ConvertAll(c => new QuestionChoiceVersionDto
                    {
                        ChoiceText = c.ChoiceText,
                        IsCorrectAnswer = c.IsCorrectAnswer,
                        AttachmentFileName = c.AttachmentFileName
                    })
                    : [],
                MatchingPairQuestionItems = matchingPairsByKey != null && matchingPairsByKey.TryGetValue((v.QuestionDetailsId, v.VersionNumber), out var pairs)
                    ? pairs.ConvertAll(p => new MatchingPairQuestionItemVersionDto
                    {
                        Id = p.Id,
                        VersionNumber = p.VersionNumber,
                        Body = p.Body,
                        ColumnOrder = p.ColumnOrder,
                        IsDataSource = p.IsDataSource
                    })
                    : [],
                SegmentQuestionPropertiesVersionDto = v.SegmentQuestionPropertiesVersions == null ? null : new SegmentQuestionPropertiesVersionDto
                {
                    OrderNumber = v.SegmentQuestionPropertiesVersions.OrderNumber,
                    WordsCount = v.SegmentQuestionPropertiesVersions.WordsCount,
                    SegmentAudioUrl = v.SegmentQuestionPropertiesVersions.SegmentAudioUrl,
                    HasScore = v.SegmentQuestionPropertiesVersions.HasScore,
                    ResponseTime = v.SegmentQuestionPropertiesVersions.ResponseTime,
                    SegmentQuestionResponseType = v.SegmentQuestionPropertiesVersions.SegmentQuestionResponseType,
                    ThinkingTime = v.SegmentQuestionPropertiesVersions.ThinkingTime
                }
            };
        }

        private ApiResponse EmptyVersionsResponse() =>
            _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Empty,
                new List<QuestionDetailsVersionDto>()
            );

        private async Task<ApiResponse> ValidateQuestionDeltaAsync(List<UpdateQuestionDeltaDto> dtos)
        {
            var codes = dtos.ConvertAll(d => d.QuestionCode);

            var questionDictionary = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .Where(q => codes.Contains(q.Code))
                .Include(q => q.DifficultyLevel)
                .AsNoTracking()
                .GroupBy(q => q.Code)
                .ToDictionaryAsync(g => g.Key, g => g.First());

            var usedInPaper = await GetCodesUsedInPaperAsync(questionDictionary);

            var rows = new List<QuestionDeltaRowResultDto>();

            foreach (var dto in dtos)
            {
                if (!questionDictionary.TryGetValue(dto.QuestionCode, out var question))
                {
                    rows.Add(new QuestionDeltaRowResultDto(
                        RowNumber: dto.RowNumber,
                        QuestionCode: dto.QuestionCode,
                        DeltaValue: dto.DeltaValue,
                        Status: QuestionDeltaRowStatus.Error,
                        Message: Resource.QuestionNotFound
                    ));

                    continue;
                }

                if (usedInPaper.Contains(dto.QuestionCode))
                {
                    rows.Add(new QuestionDeltaRowResultDto(
                        RowNumber: dto.RowNumber,
                        QuestionCode: dto.QuestionCode,
                        DeltaValue: dto.DeltaValue,
                        Status: QuestionDeltaRowStatus.Error,
                        Message: Resource.UsedInPaper
                    ));

                    continue;
                }

                bool outOfRange = dto.DeltaValue < question.DifficultyLevel.FromDelta || dto.DeltaValue > question.DifficultyLevel.ToDelta;

                rows.Add(new QuestionDeltaRowResultDto(
                    RowNumber: dto.RowNumber,
                    QuestionCode: dto.QuestionCode,
                    DeltaValue: dto.DeltaValue,
                    Status: outOfRange ? QuestionDeltaRowStatus.Warning : QuestionDeltaRowStatus.Valid,
                    Message: outOfRange ? Resource.DeltaOutOfRange : null
                ));
            }

            bool hasErrors = rows.Any(r => r.Status == QuestionDeltaRowStatus.Error);
            bool hasWarnings = rows.Any(r => r.Status == QuestionDeltaRowStatus.Warning);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                new QuestionDeltaValidationResponseDto(
                    CanProceed: !hasErrors,
                    HasWarnings: hasWarnings,
                    Rows: rows
                )
            );
        }

        private async Task<HashSet<string>> GetCodesUsedInPaperAsync(Dictionary<string, QuestionMetadata> questionDictionary)
        {
            var questionIds = questionDictionary.Values
                .Select(q => q.Id)
                .ToList();

            if (questionIds.Count == 0)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var parameters = new List<(string Name, object Value)>
            {
                ("pQuestionIds", JsonSerializer.Serialize(questionIds))
            };

            var result = await _commonService
                ._unitOfWork
                .ExecuteStoredProcedureAsync<UsedQuestionIdDto>("CheckQuestionsUsedInPaper", parameters);

            var usedIds = result
                .Select(x => x.QuestionId)
                .ToHashSet();

            return questionDictionary
                .Where(kv => usedIds.Contains(kv.Value.Id))
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        #region Helper Methods For Copying Questions

        private Dictionary<long, long> BuildChoiceIdMap(QuestionMetadata oldQuestion, QuestionMetadata copiedQuestion)
        {
            var map = new Dictionary<long, long>();
            var oldDetails = oldQuestion.QuestionDetails?.ToList() ?? [];
            var newDetails = copiedQuestion.QuestionDetails?.ToList() ?? [];

            for (int i = 0; i < Math.Min(oldDetails.Count, newDetails.Count); i++)
            {
                var oldChoices = oldDetails[i].QuestionsChoices?.ToList() ?? [];
                var newChoices = newDetails[i].QuestionsChoices?.ToList() ?? [];
                for (int j = 0; j < Math.Min(oldChoices.Count, newChoices.Count); j++)
                    map[oldChoices[j].Id] = newChoices[j].Id;
            }
            return map;
        }

        private void RemapModelAnswerChoiceIds(ICollection<QuestionDetails>? details, Dictionary<long, long> choiceIdMap)
        {
            if (details == null || choiceIdMap.Count == 0) return;

            foreach (var detail in details.Where(d => !string.IsNullOrWhiteSpace(d.ModelAnswer)))
            {
                try
                {
                    var tokens = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(detail.ModelAnswer!);
                    if (tokens == null) continue;

                    bool changed = false;
                    foreach (var token in tokens)
                    {
                        foreach (var key in new[] { "ChoiceId", "LeftChoiceId", "RightChoiceId" })
                        {
                            if (token.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.Number
                                && choiceIdMap.TryGetValue(el.GetInt64(), out var newId))
                            {
                                token[key] = JsonSerializer.SerializeToElement(newId);
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                        detail.ModelAnswer = JsonSerializer.Serialize(tokens);
                }
                catch (JsonException) { }
            }
        }

        #endregion
    }
}
