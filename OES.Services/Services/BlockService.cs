using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.AutoPermissionAssignment.Request;
using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Section;
using OES.Helper.Dtos.Stage;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Services.ParallelService;
using SharedHelper.Enums;
using SharedHelper.RolesNames;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class BlockService(
        IAutoPermissionAssignmentService _autoPermissionAssignmentService,
        ICommonService _commonService,
        FilterParamsValues _filterParamsValues,
        IMapper _mapper,
        IPaperService _paperService,
        ParallelQueryService _parallelQueryService
    ) : IBlockService
    {
        public async Task<ApiResponse> GetAllBlockPaginatedAsync(long? languageId, PaginationSearchModel paginationSearchModel)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<Block, long>()
                .GetAll(Including: $"{nameof(Block.DifficultyLevel)},{nameof(Block.DeltaType)},{nameof(Block.QuestionCategory)},{nameof(Block.Questions)},{nameof(Block.PaperBlocks)}")
                .AsNoTracking();

            bool isSuperAdmin =
                _filterParamsValues.SsoUserRoles.Exists(x => x.Name == AdminRoles.SuperAdmin) ||
                _filterParamsValues.SsoUserRoles.Exists(x => x.Name == AdminRoles.Entity_Admin);

            if (!isSuperAdmin)
            {
                var userGroupIds = _filterParamsValues
                    .OesUserGroupsAndRoles
                    .Select(g => g.GroupId)
                    .Distinct()
                    .ToList();

                query = query.Where(b =>
                    b.BlockGroups.Any(bg =>
                        !bg.IsDeleted &&
                        userGroupIds.Contains(bg.OESGroupId)
                    )
                );
            }

            if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey))
            {
                if (paginationSearchModel.SearchInName)
                {
                    query = query.Where(a => a.Name.Contains(paginationSearchModel.SearchKey));
                }
                else
                {
                    query = query.Where(a => a.Code.Contains(paginationSearchModel.SearchKey));
                }
            }

            if (paginationSearchModel.FromDate.HasValue)
            {
                query = query.Where(a => a.CreationDate >= paginationSearchModel.FromDate.Value);
            }

            if (paginationSearchModel.ToDate.HasValue)
            {
                query = query.Where(a => a.CreationDate <= paginationSearchModel.ToDate.Value);
            }

            if (paginationSearchModel.FilterObj is JsonElement jsonElement)
            {
                var filter = jsonElement.Deserialize<BlockFilterPaginationModel>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (filter != null)
                {
                    if (filter._AdaptiveSubtype != 0)
                    {
                        var autoCorrectQuestionTypes = new List<long>
                        {
                            (long)Helper.Enums.QuestionType.MCQ,
                            (long)Helper.Enums.QuestionType.MultipleCorrectAnswers,
                            (long)Helper.Enums.QuestionType.TrueAndFalse,
                            (long)Helper.Enums.QuestionType.FillInTheBlank
                        };

                        if (filter._AdaptiveSubtype == AdaptivePaperSubtype.MST)
                        {
                            query = query.Where(b => b.Questions.All(q => autoCorrectQuestionTypes.Contains(q.QuestionMetadata.QuestionTypeId)));
                        }
                        else if (filter._AdaptiveSubtype == AdaptivePaperSubtype.STEP)
                        {
                            query = query.Where(b => b.Questions.All(q =>
                                autoCorrectQuestionTypes.Contains(q.QuestionMetadata.QuestionTypeId) ||
                                (q.QuestionMetadata.QuestionTypeId == (long)Helper.Enums.QuestionType.Comprehension &&
                                 q.QuestionMetadata.SubQuestions.All(c => autoCorrectQuestionTypes.Contains(c.QuestionTypeId))) ||
                                (filter._IsStepPlus && q.QuestionMetadata.QuestionTypeId == (long)Helper.Enums.QuestionType.Segment)
                            ));
                        }
                    }
                    if (filter._SelectedDifficultyLevel > 0)
                    {
                        query = query.Where(q => q.DifficultyLevelId == filter._SelectedDifficultyLevel);
                    }
                    if (filter._SelectedBlockType > 0)
                    {
                        query = query.Where(q => q.QuestionCategoryId == filter._SelectedBlockType);
                    }
                    if (languageId > 0)
                    {
                        query = query.Where(b => b.LanguageId == languageId);
                    }
                }
            }

            query = paginationSearchModel.OrderBy == SearchInKey.DESC ? query.OrderByDescending(x => x.CreationDate) : query.OrderBy(x => x.CreationDate);

            var totalRecords = await query.CountAsync();

            if (totalRecords == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoBlocksFound);

            var paginatedBlocks = await query
                .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                .Take(paginationSearchModel.PageSize)
                .ToListAsync();

            if (paginatedBlocks.Count == 0)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoMoreBlocksFound);

            var BlockssDto = _mapper.Map<List<GetBlockResponseDto>>(paginatedBlocks);

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                Resource.Questions,
                                new CustomTableData<GetBlockResponseDto>([.. BlockssDto], totalRecords));
        }

        public async Task<ApiResponse> GetBlockDataById(long id)
        {
            var targetBlockExists = await _commonService._unitOfWork.Repository<Block, long>().IsExistAsync(e => e.Id == id);

            if (targetBlockExists)
            {
                const string includes = "DifficultyLevel," +
                                        "QuestionCategory," +
                                        "DeltaType," +
                                        "Questions.QuestionMetadata.QuestionType," +
                                        "Questions.QuestionMetadata.ItemBank," +
                                        "Questions.QuestionMetadata.QuestionDetails.Language," +
                                        "Questions.QuestionMetadata.Subject," +
                                        "Questions.QuestionMetadata.QuestionCategory," +
                                        "Questions.QuestionMetadata.DifficultyProfile," +
                                        "Questions.QuestionMetadata.DifficultyLevel," +
                                        "PaperBlocks";

                var block = await _commonService
                    ._unitOfWork
                    .Repository<Block, long>()
                    .GetObjAsync(e => e.Id == id, Including: includes);

                if (block is null)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.NotFound,
                                        HttpStatusCode.NotFound,
                                        Resource.BlockNotFound);
                }

                List<ApprovedQuestionsPaginationDto> blockQuestions = [];

                blockQuestions.AddRange(block
                    .Questions
                    .Select(question => new ApprovedQuestionsPaginationDto(
                            question.QuestionMetadata.Id,
                            question.QuestionMetadata.Code,
                            question.QuestionMetadata.Subject.Name,
                            question.QuestionMetadata.QuestionType.Name,
                            question.QuestionMetadata.QuestionCategory.Name,
                            question.QuestionMetadata.ItemBank.Name,
                            question.QuestionMetadata.DifficultyProfile.Name,
                            question.QuestionMetadata.DifficultyLevel.Name,
                            question.QuestionMetadata.QuestionDetails.FirstOrDefault(x => x.LanguageId == block.LanguageId)?.Body,
                            question.QuestionMetadata.QuestionDetails.FirstOrDefault(x => x.LanguageId == block.LanguageId)?.Language?.Name
                    ))
                );

                var blockResponseDto = new GetBlockResponseDto(block.Id,
                                                               block.Name,
                                                               block.Code,
                                                               block.Description,
                                                               block.DeltaTypeId,
                                                               block.LanguageId,
                                                               block.DeltaType.Name,
                                                               block.QuestionCategoryId,
                                                               block.QuestionCategory.Name,
                                                               block.DifficultyLevel.DifficultyProfileId,
                                                               block.DifficultyLevelId,
                                                               block.DifficultyLevel.Name,
                                                               blockQuestions,
                                                               block.Questions.Count,
                                                               block.PaperBlocks.Count > 0,
                                                               block.ConsiderDifficultyLevel);

                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    null,
                                    blockResponseDto);
            }
            else
            {
                return _commonService
                            ._apiResponse
                            .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                            HttpStatusCode.InternalServerError,
                                            Resource.AnErrorOccurredWhileFetchingTheBlock);
            }
        }

        public async Task<ApiResponse> GetPaperBlocksByPaperIdAsync(long paperId)
        {
            if (paperId == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.paperIdIsNull
                );
            }

            var repository = _commonService._unitOfWork.Repository<PaperFormBlock, long>();

            var paperBlocks = await repository
                .Query()
                .AsNoTracking()
                .Where(b => b.PaperId == paperId)
                .Select(pb => new GetPaperBlockResponseDto(
                    pb.Block.Id,
                    pb.Block.Name,
                    pb.Block.DifficultyLevelId,
                    pb.Block.DeltaTypeId,
                    pb.Block.DeltaType.Name,
                    pb.Block.QuestionCategoryId,
                    pb.Block.Questions.Select(q => q.QuestionMetadataId).Distinct().ToList()
                ))
                .ToListAsync();

            if (paperBlocks.Count > 0)
            {
                return _commonService
                       ._apiResponse
                       .GetApiResponse(CustomCodeStatus.Success,
                                       HttpStatusCode.OK,
                                       Resource.Successfully,
                                       paperBlocks);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Failure,
                        HttpStatusCode.BadRequest,
                        Resource.paperHasNoBlocks
                );
            }
        }

        public async Task<ApiResponse> GetStagesBlocksByPaperIdAsync(long paperId)
        {
            if (paperId == 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.paperIdIsNull);
            }

            var form = await _commonService._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(f => f.PaperId == paperId);

            if (form == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.FormNotFound);
            }

            const string includes = $"{nameof(PaperFormBlock.Block)}," +
                                    $"{nameof(PaperFormBlock.Block)}.{nameof(Block.DifficultyLevel)}," +
                                    $"{nameof(PaperFormBlock.Block)}.{nameof(Block.DeltaType)}," +
                                    $"{nameof(PaperFormBlock.Block)}.{nameof(Block.Questions)}," +
                                    $"{nameof(PaperFormBlock.Block)}.{nameof(Block.Questions)}.{nameof(BlockQuestion.QuestionMetadata)}," +
                                    $"{nameof(PaperFormBlock.Block)}.{nameof(Block.Questions)}.{nameof(BlockQuestion.QuestionMetadata)}.{nameof(QuestionMetadata.QuestionType)}," +
                                    $"{nameof(PaperFormBlock.Block)}.{nameof(Block.Questions)}.{nameof(BlockQuestion.QuestionMetadata)}.{nameof(QuestionMetadata.SubQuestions)}," +
                                    $"{nameof(PaperFormBlock.Block)}.{nameof(Block.QuestionCategory)}";

            var paperBlocks = await _commonService
                ._unitOfWork
                .Repository<PaperFormBlock, long>()
                .GetAllAsync(b => b.FormId == form.Id,
                     Including: includes,
                     asNoTracking: true
                );

            var stages = await _commonService._unitOfWork
                .Repository<Stage, long>()
                .GetAllAsync(s => s.FormId == form.Id,
                            OrderBy: q => q.OrderBy(s => s.Order),
                            Including: $"{nameof(Stage.AdaptiveSections)}," +
                                       $"{nameof(Stage.AdaptiveSections)}.{nameof(AdaptiveSection.InstructionSectionTemplate)}",
                            asNoTracking: true
                );

            var sections = stages.SelectMany(s => s.AdaptiveSections).OrderBy(s => s.Order).ToList();

            var paperStagesBlocks = new PaperStagesBlocksDto
            {
                Stages = _commonService._mapper.Map<List<StageDto>>(stages) ?? [],

                Blocks = _commonService._mapper.Map<List<GetChoosenBlocksDto>>(paperBlocks) ?? [],

                AdaptiveSections = _commonService._mapper.Map<List<AdaptiveSectionDto>>(sections) ?? [],

                FormId = form.Id
            };

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                              HttpStatusCode.OK,
                                                              Resource.Successfully,
                                                              paperStagesBlocks);
        }

        public async Task<ApiResponse> CreateOrUpdatePaperBlockDistributionAsync(PaperStagesBlocksDto paperStagesBlocksDto)
        {
            if (paperStagesBlocksDto == null)
            {
                return _commonService
                      ._apiResponse
                      .GetApiResponse(CustomCodeStatus.Failure,
                                      HttpStatusCode.BadRequest,
                                      Resource.InvalidDataProvided);
            }

            var paperFormBlockRepo = _commonService._unitOfWork.Repository<PaperFormBlock, long>();

            var sectionRepo = _commonService._unitOfWork.Repository<AdaptiveSection, long>();

            var stageRepo = _commonService._unitOfWork.Repository<Stage, long>();

            var blocksTask = _parallelQueryService
                .ExecuteReadAsync<PaperFormBlock, long, IEnumerable<PaperFormBlock>>(repo => repo.GetAllAsync(pb => pb.FormId == paperStagesBlocksDto.FormId));

            var sectionsTask = _parallelQueryService
                .ExecuteReadAsync<AdaptiveSection, long, IEnumerable<AdaptiveSection>>(repo => repo.GetAllAsync(s => s.Stage.FormId == paperStagesBlocksDto.FormId));

            var stagesTask = _parallelQueryService
                .ExecuteReadAsync<Stage, long, IEnumerable<Stage>>(repo => repo.GetAllAsync(s => s.FormId == paperStagesBlocksDto.FormId));

            await Task.WhenAll(blocksTask, sectionsTask, stagesTask);

            var existingPaperBlocks = (await blocksTask).ToList();

            var existingSections = (await sectionsTask).ToList();

            var existingStages = (await stagesTask).ToList();

            var dbBlocksMap = existingPaperBlocks.ToDictionary(b => b.BlockId);

            foreach (var block in existingPaperBlocks)
            {
                block.AdaptiveSectionId = null;
                block.AdaptiveSection = null;
                block.Distribution = false;
            }

            if (existingPaperBlocks.Count > 0)
            {
                paperFormBlockRepo.UpdateRange(existingPaperBlocks);

                await _commonService._unitOfWork.Complete();
            }

            if (existingSections.Count > 0)
            {
                sectionRepo.DeleteRange(existingSections);
            }

            Dictionary<long, AdaptiveSection> sectionMap = [];

            List<AdaptiveSection> sectionsToAdd = [];

            List<Stage> modifiedStages = [];

            foreach (var stageDto in paperStagesBlocksDto.Stages)
            {
                var dbStage = existingStages.FirstOrDefault(s => s.Id == stageDto.Id);

                if (dbStage != null)
                {
                    dbStage.RenderedPartName = stageDto.RenderedPartName;

                    dbStage.TimeInMinutes = stageDto.TimeInMinutes;

                    dbStage.InstructionSectionTemplateId = stageDto.InstructionSectionAdaptiveId;

                    modifiedStages.Add(dbStage);
                }

                var sectionsForStage = paperStagesBlocksDto.AdaptiveSections.Where(s => s.StageId == stageDto.Id).ToList();

                int order = 1;

                foreach (var secDto in sectionsForStage)
                {
                    var newSection = new AdaptiveSection
                    {
                        Name = secDto.Name,
                        AdaptivePaperSubtype = secDto.AdaptivePaperSubtype,
                        StageId = stageDto.Id,
                        TimeInMinutes = secDto.TimeInMinutes,
                        Order = order++,
                        UnScored = secDto.UnScored,
                        DifficultyLevelId = secDto.DifficultyLevelId,
                        InstructionSectionTemplateId = secDto.InstructionSectionAdaptiveId,
                        PaperBlocks = []
                    };

                    sectionsToAdd.Add(newSection);

                    sectionMap[secDto.Id] = newSection;
                }
            }

            foreach (var blockDto in paperStagesBlocksDto.Blocks)
            {
                if (dbBlocksMap.TryGetValue(blockDto.BlockId, out var dbBlock) &&
                    blockDto.AdaptiveSectionId.HasValue &&
                    sectionMap.TryGetValue(blockDto.AdaptiveSectionId.Value, out var newSectionEntity)
                )
                {
                    dbBlock.AdaptiveSection = newSectionEntity;

                    dbBlock.Distribution = true;
                }
            }

            if (modifiedStages.Count > 0)
            {
                stageRepo.UpdateRange(modifiedStages);
            }

            if (sectionsToAdd.Count > 0)
            {
                sectionRepo.AddRange(sectionsToAdd);
            }

            if (existingPaperBlocks.Count > 0)
            {
                paperFormBlockRepo.UpdateRange(existingPaperBlocks);
            }

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.BlockUpdatedSuccessfully
            );
        }

        public async Task<ApiResponse> AddNewBlock(CreateOrUpdateBlockRequestDto blockDto)
        {
            if (blockDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Failure,
                                                                  HttpStatusCode.BadRequest,
                                                                  Resource.InvalidDataProvided);
            }

            var blockRepo = _commonService._unitOfWork.Repository<Block, long>();

            List<string> errors = [];

            var trimmedToLowerName = blockDto.Name?.Trim().ToLower();
            var trimmedToLowerCode = blockDto.Code?.Trim().ToLower();

            if (await blockRepo.IsExistAsync(x => x.Name.Trim().ToLower() == trimmedToLowerName))
                errors.Add($"{Resource.Name}: {blockDto.Name}");

            if (await blockRepo.IsExistAsync(x => x.Code.Trim().ToLower() == trimmedToLowerCode))
                errors.Add($"{Resource.Code}: {blockDto.Code}");

            if (errors.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    string.Format(Resource.DuplicatedBlockFields, string.Join(" , ", errors))
                );
            }

            var categoryRepo = _commonService._unitOfWork.Repository<QuestionCategory, long>();

            var category = await categoryRepo.GetByIdAsync(blockDto.BlockTypeId);

            if (category == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CategoryNotFound
                );
            }

            // Validate selected questions
            var questionsRepo = _commonService._unitOfWork.Repository<QuestionMetadata, long>();

            var selectedQuestions = (await questionsRepo
                .GetAllAsync(q => blockDto.QuestionsIds.Contains(q.Id), null))
                .ToList();

            var exhaustedQuestions = selectedQuestions
                .Where(q => q.CurrentExhaustionCount >= q.QuestionsExhaustionCount)
                .ToList();

            if (exhaustedQuestions.Count > 0)
            {
                var exhaustedCodes = string.Join(", ", exhaustedQuestions.Select(q => q.Code));

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.BlockMismatch,
                    HttpStatusCode.BadRequest,
                    string.Format(Resource.ExhaustionLimitReached, exhaustedCodes)
                );
            }

            var validQuestions = selectedQuestions
                .Where(q => q.CurrentExhaustionCount < q.QuestionsExhaustionCount)
                .ToList();

            if (blockDto.ConsiderDifficultyLevel)
            {
                foreach (var question in validQuestions)
                {
                    // Check DifficultyLevel
                    if (question.DifficultyLevelId != blockDto.DifficultyLevelId)
                    {
                        return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.BlockMismatch,
                            HttpStatusCode.BadRequest,
                            Resource.DeltaLevelMismatchInSelectedQuestions);
                    }
                }
            }

            // All validations passed, map and save

            var newBlock = _mapper.Map<Block>(blockDto);

            await _commonService._unitOfWork.Repository<Block, long>().AddAsync(newBlock);

            await _commonService._unitOfWork.Complete();

            await _autoPermissionAssignmentService.AssignDefaultPermissionsForNewEntityAsync(
                new AutoPermissionAssignmentRequest
                {
                    EntityId = newBlock.Id,
                    EntityName = newBlock.Name,
                    ResourceType = ResourceType.Block,
                    EntityGroupType = typeof(BlockGroups),
                    AdditionalGroupIds = blockDto.OESGroupIds
                }
            );

            var blockGroupRepo = _commonService._unitOfWork.Repository<BlockGroups, long>();

            foreach (var gid in blockDto.OESGroupIds)
            {
                await blockGroupRepo.AddAsync(new BlockGroups
                {
                    BlockId = newBlock.Id,
                    OESGroupId = gid
                });
            }

            if (blockDto.QuestionsIds.Count > 0)
            {
                var blockQuestions = blockDto.QuestionsIds.ConvertAll(qId => new BlockQuestion
                {
                    BlockId = newBlock.Id,
                    QuestionMetadataId = qId
                });

                _commonService._unitOfWork.Repository<BlockQuestion, long>().AddRangAsync(blockQuestions);
            }

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.BlockAddedSuccessfully,
                                                                  newBlock);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.FailedToAddBlock
                );
            }
        }

        public async Task<ApiResponse> EditBlock(CreateOrUpdateBlockRequestDto blockDto)
        {
            if (blockDto == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.InvalidDataProvided
                );
            }
            var blockRepo = _commonService._unitOfWork.Repository<Block, long>();

            var block = await blockRepo
                .GetObjAsync(e => e.Id == blockDto.Id,
                             Including: $"{nameof(Block.Questions)}.{nameof(BlockQuestion.QuestionMetadata)}," +
                                        $"{nameof(Block.PaperBlocks)}.{nameof(PaperFormBlock.Paper)}," +
                                        $"{nameof(Block.BlockGroups)}.{nameof(BlockGroups.OESGroup)}");

            if (block == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.BlockNotFound
                );
            }

            List<string> errors = [];

            var trimmedToLowerName = blockDto.Name?.Trim().ToLower();
            var trimmedToLowerCode = blockDto.Code?.Trim().ToLower();

            if (await blockRepo.IsExistAsync(x => x.Id != block.Id && x.Name.Trim().ToLower() == trimmedToLowerName))
                errors.Add($"{Resource.Name}: {blockDto.Name}");

            if (await blockRepo.IsExistAsync(x => x.Id != block.Id && x.Code.Trim().ToLower() == trimmedToLowerCode))
                errors.Add($"{Resource.Code}: {blockDto.Code}");

            if (errors.Count > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.BadRequest,
                    string.Format(Resource.DuplicatedBlockFields, string.Join(" , ", errors))
                );
            }

            var categoryRepo = _commonService._unitOfWork.Repository<QuestionCategory, long>();

            var category = await categoryRepo.GetByIdAsync(blockDto.BlockTypeId);

            if (category == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CategoryNotFound
                );
            }

            // Update block properties
            block.Name = blockDto.Name;
            block.Description = blockDto.Description;
            block.Code = blockDto.Code;

            bool blockUsedInPaper = block.PaperBlocks.Any(pb =>
                pb.Paper.PaperStatus == AvailabilityStatus.Active ||
                pb.Paper.PaperCreationStatus >= PaperCreationStatus.ItemBanksOrBlocksSelected
            );

            if (blockUsedInPaper)
            {
                return await _commonService._unitOfWork.Complete() > 0
                    ? _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        Resource.BlockUpdatedSuccessfully,
                        block
                    )
                    : _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.InternalServerError,
                        Resource.BlockUsed
                    );
            }

            block.QuestionCategoryId = blockDto.BlockTypeId;
            block.DeltaTypeId = blockDto.DeltaTypeId;
            block.DifficultyLevelId = blockDto.DifficultyLevelId;
            block.LanguageId = blockDto.LanguageId;
            block.ConsiderDifficultyLevel = blockDto.ConsiderDifficultyLevel;

            var existingBlockQuestions = block.Questions.ToList();

            if (existingBlockQuestions.Count > 0)
            {
                _commonService._unitOfWork.Repository<BlockQuestion, long>().DeleteRange(existingBlockQuestions);
            }

            // Insert new `BlocksQuestions` records
            if (blockDto.QuestionsIds.Count > 0)
            {
                var newBlockQuestions = blockDto.QuestionsIds.ConvertAll(qId => new BlockQuestion
                {
                    BlockId = block.Id,
                    QuestionMetadataId = qId
                });

                _commonService._unitOfWork.Repository<BlockQuestion, long>().AddRangAsync(newBlockQuestions);
            }

            var blockGroupRepo = _commonService._unitOfWork.Repository<BlockGroups, long>();

            var ownerGroup = block.BlockGroups.FirstOrDefault(g => g.OESGroup.AutoCreatedForUser);

            var ownerGroupId = ownerGroup?.OESGroupId ?? Guid.Empty;

            var requestedIds = blockDto.OESGroupIds?.ToHashSet() ?? [];

            var toRemove = block.BlockGroups
                .Where(g => g.OESGroupId != ownerGroupId && !requestedIds.Contains(g.OESGroupId))
                .ToList();

            foreach (var removed in toRemove)
            {
                block.BlockGroups.Remove(removed);
            }

            var existingIds = block.BlockGroups
                .Select(g => g.OESGroupId)
                .ToHashSet();

            var toAdd = requestedIds
                .Where(id => id != ownerGroupId && !existingIds.Contains(id))
                .ToList();

            foreach (var gid in toAdd)
            {
                await blockGroupRepo.AddAsync(new BlockGroups
                {
                    BlockId = block.Id,
                    OESGroupId = gid
                });
            }

            // Save all changes
            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.BlockUpdatedSuccessfully,
                    block
                );
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.FailedToUpdateTheBlock
                );
            }
        }

        public async Task<ApiResponse> DeleteBlockAsync(long id)
        {
            var isUsedInPaper = await _commonService
                ._unitOfWork
                .Repository<PaperFormBlock, long>()
                .IsExistAsync(pb => pb.BlockId == id);

            if (isUsedInPaper)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                    HttpStatusCode.BadRequest,
                                    Resource.BlockCannotBeDeleted);
            }

            var blockRepo = _commonService._unitOfWork.Repository<Block, long>();

            var block = await blockRepo.GetObjAsync(b => b.Id == id, "Questions");

            if (block == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound,
                                    HttpStatusCode.NotFound,
                                    string.Format(Resource.BlockWithIdNotFound, id));
            }

            blockRepo.SoftDeleteRecursive(block);

            var result = await _commonService._unitOfWork.Complete();

            if (result > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    Resource.BlockhasbeenDeletedSuccessfully,
                                    block);
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.BadRequest,
                                Resource.FailedToDeleteBlock);
        }

        public async Task<ApiResponse> AddOrUpdatePaperBlocksAsync(PaperBlockCreationDto paperBlockCreationDto)
        {
            var validationResult = ValidatePaperBlocksData(paperBlockCreationDto);

            if (!validationResult.ValidationSucceeded)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    validationResult.Message
                );
            }

            var form = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(f => f.PaperId == paperBlockCreationDto.PaperId);

            if (form == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "Default Form not found for this paper."
                );
            }

            var paperBlockRepo = _commonService
                ._unitOfWork
                .Repository<PaperFormBlock, long>();

            var existingPaperBlocks = await paperBlockRepo
                .GetAllAsync(pb => pb.FormId == form.Id, asNoTracking: true);

            var existingBlockIds = existingPaperBlocks.Select(x => x.BlockId).ToList();

            var newPaperblocksIdsToAdd = paperBlockCreationDto.SelectedBlocksIds.Except(existingBlockIds).ToList();

            var existingPaperblocksIdsToRemove = existingBlockIds.Except(paperBlockCreationDto.SelectedBlocksIds).ToList();

            if (newPaperblocksIdsToAdd.Count > 0)
            {
                var newPaperBlocksToAdd = newPaperblocksIdsToAdd.ConvertAll(blockId => new PaperFormBlock
                {
                    PaperId = paperBlockCreationDto.PaperId,
                    FormId = form.Id,
                    BlockId = blockId,
                });

                paperBlockRepo.AddRangAsync(newPaperBlocksToAdd);
            }

            if (existingPaperblocksIdsToRemove.Count > 0)
            {
                var entitiesToRemove = await paperBlockRepo
                    .GetAllAsync(pb => pb.FormId == form.Id && existingPaperblocksIdsToRemove.Contains(pb.BlockId));

                paperBlockRepo.DeleteRange(entitiesToRemove);
            }

            await _commonService._unitOfWork.Complete();

            var updatePaperCreationStatusDto = new UpdatePaperCreationStatusRequestDto(paperBlockCreationDto.PaperId, PaperCreationStatus.ItemBanksOrBlocksSelected);

            await _paperService.UpdatePaperCreationStatusAsync(updatePaperCreationStatusDto);

            if (await _commonService._unitOfWork.Complete() >= 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.PaperBlocksSelectedSuccessfully
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.ErrorInSavingPaperBlocks
            );
        }

        public async Task<ApiResponse> GetUserBlockGroupsAsync()
        {
            var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";

            var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

            var groups = await groupRepo.GetAllAsync(g =>
                !g.IsDeleted &&
                !g.IsTemplate &&
                !g.IsPredefined &&
                !g.AutoCreatedForUser &&
                g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.Block) &&
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

        public async Task<ApiResponse> GetBlockGroupsAsync(long blockId)
        {
            var blockGroups = await _commonService
                ._unitOfWork
                .Repository<BlockGroups, long>()
                .GetAllAsync(
                    x => x.BlockId == blockId &&
                         !x.OESGroup.IsTemplate,
                    Including: nameof(BlockGroups.OESGroup)
                );

            var blockGroupsDto = new BlockGroupsDto();

            blockGroups.ToList().ForEach(ibg =>
                blockGroupsDto.GroupsIds.Add(ibg.OESGroupId));

            blockGroupsDto.OwnerGroupId = blockGroups
                .FirstOrDefault(x =>
                    x.OESGroup != null &&
                    x.OESGroup.AutoCreatedForUser
                )?.OESGroupId;

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.SuccessfulFetching,
                blockGroupsDto
            );
        }


        #region Helper Methods

        private static (bool ValidationSucceeded, string Message) ValidatePaperBlocksData(PaperBlockCreationDto paperBlockCreationDto)
        {
            if (paperBlockCreationDto.PaperId == 0)
            {
                return (false, Resource.PaperIdCannotBeEmpty);
            }

            if (paperBlockCreationDto.SelectedBlocksIds.Count == 0)
            {
                return (false, Resource.SelectAtLeastOneBlock);
            }

            return (true, string.Empty);
        }

        #endregion Helper Methods
    }
}