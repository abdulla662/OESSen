using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.Paper.ImportQuestionsRequestDto;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionIndicators;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class QuestionController : OESBaseController
    {
        private readonly IQuestionService _questionService;
        private readonly IQuestionDistributionValidationService _questionDistributionValidationService;


        /// <summary>
        /// Initializes a new instance of the <see cref="QuestionController"/> class.
        /// </summary>
        /// <param name="questionService">The service for handling question-related operations.</param>
        public QuestionController(IQuestionService questionService, IQuestionDistributionValidationService questionDistributionValidationService)
        {
            _questionService = questionService;
            _questionDistributionValidationService = questionDistributionValidationService;
        }


        /// <summary>
        /// Adds a new question.
        /// </summary>
        /// <param name="questionDto">The data transfer object containing the details of the new question.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AddNewQuestion")]
        public async Task<ApiResponse> AddQuestionMetadataAsync(QuestionMetadataAdditionOrUpdateDto questionDto)
        {
            return await _questionService.AddQuestionMetadataAsync(questionDto);
        }


        /// <summary>
        /// Adds a new question.
        /// </summary>
        /// <param name="questionTemplateDto">The data transfer object containing the details of the new question.</param>
        /// <returns>An <see cref="ApiResponse"/> indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AddTemplateQuestion")]
        public async Task<ApiResponse> AddQuestionTemplateAsync(QuestionCreationTemplateDto questionTemplateDto)
        {
            return await _questionService.AddQuestionTemplateAsync(questionTemplateDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddTemplateAIQuestion")]
        public async Task<ApiResponse> AddTemplateAIQuestionAsync(AIQuestionTemplateCreationDto dto)
        {
            return await _questionService.AddTemplateAIQuestionAsync(dto);
        }


        /// <summary>
        /// Adds a comprehension sub-question to the system.
        /// </summary>
        /// <param name="comprehensionSubQuestionDto">The data transfer object containing details of the comprehension sub-question.</param>
        /// <returns>An ApiResponse indicating the success or failure of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AddComprehensionSubQuestion")]
        public async Task<ApiResponse> AddComprehensionSubQuestionAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            return await _questionService.AddComprehensionSubQuestionAsync(comprehensionSubQuestionDto);
        }


        /// <summary>
        /// Retrieves all question languages based on pagination and a specific question metadata ID.
        /// </summary>
        /// <param name="paginationModel">The model containing pagination and search criteria.</param>
        /// <param name="QuestionMetaDataId">The unique identifier for the question metadata.</param>
        /// <returns>An ApiResponse containing a list of question languages.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("GetAllQuestionLanguage")]
        public async Task<ApiResponse> GetAllQuestionLanguage(PaginationSearchModel paginationModel, long QuestionMetaDataId)
        {
            return await _questionService.GetAllQuestionLanguage(paginationModel, QuestionMetaDataId);
        }


        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteQuestionTemplate")]
        public async Task<ApiResponse> DeleteQuestionTemplate(long templateId)
        {
            return await _questionService.DeleteQuestionTemplateAsync(templateId);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionByMetaDataIdAndLanguageId")]
        public async Task<ApiResponse> GetQuestionByMetaDataIdAndLanguageId(long metaDataId, long languageId)
        {
            return await _questionService.GetQuestionByMetaDataIdAndLanguageId(metaDataId, languageId);
        }


        /// <summary>
        /// Edits an existing question.
        /// </summary>
        /// <param name="questionMetaData">The model containing updated question.</param>
        /// <returns>An ApiResponse indicating the result of the update operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("EditQuestion")]
        public async Task<ApiResponse> EditQuestion(QuestionMetadataAdditionOrUpdateDto questionMetaData)
        {
            return await _questionService.EditQuestionMetadataAsync(questionMetaData);
        }


        /// <summary>
        /// Adds a language variant for a comprehension sub-question.
        /// </summary>
        /// <param name="comprehensionSubQuestionLanguageVariantDto">
        /// The DTO containing the details of the language variant to be added.
        /// </param>
        /// <returns>
        /// An <see cref="ApiResponse"/> indicating the result of the operation.
        /// </returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AddComprehensionSubQuestionLanguageVariant")]
        public async Task<ApiResponse> AddComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto comprehensionSubQuestionLanguageVariantDto)
        {
            return await _questionService.AddComprehensionSubQuestionLanguageVariantAsync(comprehensionSubQuestionLanguageVariantDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllQuestion")]
        public async Task<ApiResponse> GetAllQuestionAsync([FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _questionService.GetAllQuestionAsync(paginationSearchModel);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetAllStandaloneQuestions")]
        public async Task<ApiResponse> GetAllStandaloneQuestionsAsync([FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _questionService.GetStandaloneQuestionMetadataAsync(paginationSearchModel);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("GetAllQuestionByBlockId")]
        public async Task<ApiResponse> GetAllQuestionByBlockId(long blockId, [FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _questionService.GetAllQuestionByBlockId(blockId, paginationSearchModel);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllApprovedQuestionsForBlocksAsync")]
        public async Task<ApiResponse> GetAllApprovedQuestionsForBlocksAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _questionService.GetAllApprovedQuestionsForBlocksAsync(paginationSearchModel);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllQuestionFromItemBankPoint")]
        public async Task<ApiResponse> GetAllQuestionFormItemBankPoint(long paperId, [FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _questionService.GetAllQuestionFromItemBankPoint(paperId, paginationSearchModel);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllQuestionForPendingPage")]
        public async Task<ApiResponse> GetAllPendingQuestionAsync([FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _questionService.GetAllQuestionForPendingPage(paginationSearchModel);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllQuestionForPendingOnlyPage")]
        public async Task<ApiResponse> GetAllQuestionForPendingOnlyPage([FromBody] PaginationSearchModel paginationSearchModel)
        {
            return await _questionService.GetAllQuestionForPendingOnlyPage(paginationSearchModel);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddNewQuestionDetails")]
        public async Task<ApiResponse> AddNewQuestionDetailsAsync(QuestionDetailsDto questionDetailsDto)
        {
            return await _questionService.AddNewQuestionLanguageDetails(questionDetailsDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("EditQuestionDetails")]
        public async Task<ApiResponse> EditQuestionDetails(QuestionDetailsDto questionDetailsDto)
        {
            return await _questionService.EditQuestionDetails(questionDetailsDto);
        }


        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteQuestionDetails")]
        public async Task<ApiResponse> DeleteQuestionDetails(long questionDetailsId)
        {
            return await _questionService.DeleteQuestionDetails(questionDetailsId);
        }


        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteQuestionMetadata")]
        public async Task<ApiResponse> DeleteQuestionMetadata(long questionMetadataId)
        {
            return await _questionService.DeleteQuestionMetaData(questionMetadataId);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionDetailsById")]
        public async Task<ApiResponse> GetQuestionDetailsByIdAsync(long questionDetailsId)
        {
            return await _questionService.GetQuestionDetailsByIdAsync(questionDetailsId);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionMetadataById")]
        public async Task<ApiResponse> GetQuestionMetadataByIdAsync(long questionMetadataId)
        {
            return await _questionService.GetQuestionMetadataByIdAsync(questionMetadataId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("UpdateComprehensionSubQuestionLanguageVariant")]
        public async Task<ApiResponse> UpdateComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto)
        {
            return await _questionService.UpdateComprehensionSubQuestionLanguageVariantAsync(subQuestionLanguageVariantDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("ChangeQuestionCreationStatus")]
        public async Task<ApiResponse> ChangeQuestionCreationStatusAsync(UpdateQuestionCreationStatusRequestDto updateQuestionCreationStatusRequestDto)
        {
            return await _questionService.ChangeQuestionCreationStatusAsync(updateQuestionCreationStatusRequestDto);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("ChangeQuestionsCreationStatus")]
        public async Task<ApiResponse> ChangeQuestionsCreationStatusAsync(UpdateQuestionsCreationStatusRequestDto updateQuestionsCreationStatusRequestDto)
        {
            return await _questionService.ChangeQuestionsCreationStatusAsync(updateQuestionsCreationStatusRequestDto);
        }

        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteComprehensionSubQuestionLanguageVariant")]
        public async Task<ApiResponse> DeleteComprehensionSubQuestionLanguageVariantAsync(long subQuestionMetadataId, long subQuestionDetailsId)
        {
            return await _questionService.DeleteComprehensionSubQuestionLanguageVariantAsync(subQuestionMetadataId, subQuestionDetailsId);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetComprehensionSubQuestions")]
        public async Task<ApiResponse> GetComprehensionSubQuestionsAsync(long rootComprehensionQuestionMetadataId, long languageId)
        {
            return await _questionService.GetComprehensionSubQuestionsAsync(rootComprehensionQuestionMetadataId, languageId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllQuestionTemplates")]
        public async Task<ApiResponse> GetAllQuestionTemplatesAsync([FromBody] PaginationSearchModel paginationSearch, [FromQuery] bool isFromQuestionAI)
        {
            return await _questionService.GetAllQuestionTemplatesAsync(paginationSearch, isFromQuestionAI);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionTemplateById")]
        public async Task<ApiResponse> GetQuestionTemplateById(long questionTemplateId)
        {
            return await _questionService.GetQuestionTemplateByIdAsync(questionTemplateId);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetAllQuestionByItemBankId")]
        public async Task<ApiResponse> GetAllQuestionByItemBankId([FromQuery] List<long> itemBankIds)
        {
            return await _questionService.GetAllQuestionByItemBankId(itemBankIds);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetAllQuestionsByIds")]
        public async Task<ApiResponse> GetAllQuestionsByIds([FromQuery] List<long> questionIds)
        {
            return await _questionService.GetAllQuestionsByIds(questionIds);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionTypeCountsByItemBankId")]
        public async Task<ApiResponse> GetQuestionTypeCountsByItemBankId([FromQuery] long itemBankId)
        {
            return await _questionService.GetQuestionTypeCountsByItemBankId(itemBankId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("ValidateQuestionTypeAndDifficultyDistributionAsync")]
        public async Task<ApiResponse> ValidateQuestionTypeAndDifficultyDistributionAsync([FromBody] AddOrUpdateAutoSectionsDistributionsRequestDto request)
        {
            return await _questionDistributionValidationService.ValidateQuestionTypeAndDifficultyDistributionAsync(request);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetNextQuestionForQualityCheck")]
        public Task<ApiResponse> GetNextQuestionId([FromQuery] long currentId)
            => _questionService.GetNextQuestionForQualityCheckAsync(currentId);


        [OESFilter(Authorize = true)]
        [HttpGet("GetPreviousQuestionForQualityCheck")]
        public Task<ApiResponse> GetPreviousQuestionId([FromQuery] long currentId)
            => _questionService.GetPreviousQuestionForQualityCheckAsync(currentId);


        [OESFilter(Authorize = true)]
        [HttpGet("GetNextQuestionIdInSameBranch")]
        public Task<ApiResponse> GetQuestionIdInSameBranch([FromQuery] long currentId)
            => _questionService.GetNextQuestionIdInSameBranchAsync(currentId);


        [OESFilter(Authorize = true)]
        [HttpPost("ValidateQuestionsByQuestionCodes")]
        public async Task<ApiResponse> ValidateQuestionsByQuestionCodesAsync(AddMultipleQuestionsRequestDto addMultipleQuestionsRequestDto)
        {
            return await _questionService.ValidateQuestionsByQuestionCodesAsync(addMultipleQuestionsRequestDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetQuestionsByItemBankAndType")]
        public async Task<ApiResponse> GetQuestionsByItemBankAndType(MixedSelectedQuestionsNodeDto autoSelectedQuestionsNodeDto)
        {
            return await _questionService.GetQuestionsByItemBankAndType(autoSelectedQuestionsNodeDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddOrUpdateSegmentQuestion")]
        public async Task<ApiResponse> AddOrUpdateSegmentQuestionAsync([FromBody] List<SegmentQuestionDto> segmentQuestionDto)
        {
            return await _questionService.AddOrUpdateSegmentQuestionAsync(segmentQuestionDto);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetSegmentQuestionByMetaDataId")]
        public async Task<ApiResponse> GetSegmentQuestionByMetaDataId(long parentId, long languageId)
        {
            return await _questionService.GetSegmentQuestionByMetaDataIdAsync(parentId, languageId);
        }


        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteSegmentQuestionLanguageVariant")]
        public async Task<ApiResponse> DeleteSegmentQuestionLanguageVariantAsync(long segmentQuestionMetadataId, long segmentQuestionDetailsId)
        {
            return await _questionService.DeleteSegmentQuestionLanguageVariantAsync(segmentQuestionMetadataId, segmentQuestionDetailsId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddOrUpdateMatchingPairsQuestion")]
        public async Task<ApiResponse> AddOrUpdateMatchingPairsQuestionAsync(AddOrUpdateMatchingPairsRequestDto addOrUpdateMatchingPairsRequestDto)
        {
            return await _questionService.AddOrUpdateMatchingPairsQuestionAsync(addOrUpdateMatchingPairsRequestDto);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetMatchingPairsQuestion")]
        public async Task<ApiResponse> GetMatchingPairsQuestionAsync(long metadataParentId, long? languageId)
        {
            return await _questionService.GetMatchingPairsQuestionAsync(metadataParentId, languageId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddOrUpdateMatchingPairsWithDragDropQuestion")]
        public async Task<ApiResponse> AddOrUpdateMatchingPairsWithDragDropQuestionAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto request)
        {
            return await _questionService.AddOrUpdateMatchingPairsWithDragDropQuestionAsync(request);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetMatchingPairsWithDragDropQuestion")]
        public async Task<ApiResponse> GetMatchingPairsWithDragDropQuestionAsync(long metadataParentId, long? languageId)
        {
            return await _questionService.GetMatchingPairsWithDragDropQuestionAsync(metadataParentId, languageId);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetAllQuestionWithGroups")]
        public async Task<ApiResponse> GetAllQuestionGroupsAsync()
        {
            return await _questionService.GetAllQuestionWithGroupsAsync();
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionGroups")]
        public async Task<IApiResponse> GetQuestionGroupsAsync(long questionId)
        {
            return await _questionService.GetQuestionGroupsAsync(questionId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetQuestionIndicators")]
        public async Task<ApiResponse> GetQuestionAnalyticsIndicatorsAsync(PaginationSearchModel filter)
        {
            return await _questionService.GetQuestionAnalyticsIndicatorsAsync(filter);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("ExportQuestionIndicators")]
        public async Task<ApiResponse> ExportQuestionIndicatorsAsync(QuestionIndicatorExportRequestDto request)
        {
            return await _questionService.ExportQuestionIndicatorsAsync(request);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetFilteredQuestions")]
        public async Task<ApiResponse> GetFilteredQuestionsAsync(PaginationSearchModel pagination)
        {
            return await _questionService.GetFilteredQuestionsAsync(pagination);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("ExportFilteredQuestionsToExcel")]
        public async Task<IActionResult> ExportFilteredQuestionsToExcelAsync(PaginationSearchModel pagination)
        {
            var result = await _questionService.ExportFilteredQuestionsToExcelAsync(pagination);
            return File(result.Bytes, result.ContentType, result.FileName);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetQuestionAuthors")]
        public async Task<ApiResponse> GetQuestionAuthorsAsync()
        {
            return await _questionService.GetQuestionAuthorsAsync();
        }


        [HttpPost("ValidateStandaloneQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> ValidateStandaloneQuestions([FromForm] UploadFreeQuestions requestDto)
        {
            return await _questionService.ValidateStandaloneQuestionsAsync(requestDto);
        }
    }
}