using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.Paper.ImportQuestionsRequestDto;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionIndicators;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.QuestionTemplateDto;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Question
{
    public interface IBlazQuestionService
    {
        Task<ApiResponse> AddQuestionMetaData(QuestionMetadataAdditionOrUpdateDto questionMetaData);
        Task<ApiResponse> AddTemplateQuestionAsync(QuestionCreationTemplateDto questionTemplateDto);
        Task<QuestionGroupsDto> GetQuestionGroupsAsync(long questionId);
        Task<ApiResponse> AddQuestionDetails(QuestionDetailsDto questionDetailsDto);
        Task<CustomTableData<QuestionLanguageDTO>> GetAllQuestionLanguage(PaginationSearchModel pagination, long QuestionMetaDataId);
        Task<ApiResponse> EditQuestionMetaData(QuestionMetadataAdditionOrUpdateDto questionMetaData);
        Task<ApiResponse> EditQuestionDetailsAsync(QuestionDetailsDto questionDetailsDto);
        Task<ApiResponse> UpdateComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto);
        Task<ApiResponse> ChangeQuestionCreationStatusAsync(UpdateQuestionCreationStatusRequestDto updateQuestionCreationStatusRequestDto);
        Task<ApiResponse> ChangeQuestionsCreationStatusAsync(UpdateQuestionsCreationStatusRequestDto updateQuestionsCreationStatusRequestDto);
        Task<CustomTableData<QuestionMetadataPaginationDto>> GetAllQuestionAsync(PaginationSearchModel paginationSearch);
        Task<List<GetOESGroupDto>> GetAllQuestionGroupsAsync();
        Task<CustomTableData<ManualQuestionsPaginationResponseDto>> GetAllQuestionFormItemBankPoint(long paperId, PaginationSearchModel paginationSearch);
        Task<CustomTableData<PendingQuestionPaginationDto>> GetAllQuestionForPendingPage(PaginationSearchModel paginationSearch);
        Task<ApiResponse> AddComprehensionSubQuestionAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto);
        Task<ApiResponse> AddComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto);
        Task<ApiResponse> GetQuestionByMetaDataIdAndLanguageId(long metaDataId, long languageId);
        Task<ApiResponse> GetQuestionDetailsByIdAsync(long questionDetailsId);
        Task<ApiResponse> GetQuestionMetadataByIdAsync(long questionMetadataId);
        Task<ApiResponse> GetComprehensionSubQuestionsAsync(long rootComprehensionQuestionMetadataId, long languageId);
        Task<ApiResponse> DeleteQuestionDetailsByIdAsync(long questionDetailsId);
        Task<ApiResponse> DeleteQuestionTemplateAsync(long templateId);
        Task<ApiResponse> DeleteQuestionMetadataByIdAsync(long questionMetadataId);
        Task<ApiResponse> DeleteComprehensionSubQuestionLanguageVariantAsync(long subQuestionMetadataId, long subQuestionDetailsId);
        Task<CustomTableData<QuestionTemplateDto>> GetAllQuestionTemplateAsync(QuestionPaginationSearchRequest questionPaginationSearchRequest);
        Task<ApiResponse> GetQuestionTemplateById(long questionTemplateId);
        Task<CustomTableData<PendingQuestionPaginationDto>> GetAllQuestionForPendingOnlyPage(PaginationSearchModel paginationSearch);
        Task<ApiResponse> BypassQuestionsAsync(BypassQuestionsDto bypassQuestionsDto);
        Task<CustomTableData<BlockQuestionsDetailsPaginationDto>> GetAllQuestionByBlockId(long blockId, PaginationSearchModel paginationSearch);
        Task<CustomTableData<ApprovedQuestionsPaginationDto>> GetAllApprovedQuestionsForBlocksAsync(PaginationSearchModel paginationSearch);
        Task<long?> GetNextQuestionIdAsync(long currentId);
        Task<long?> GetPreviousQuestionIdAsync(long currentId);
        Task<long?> GetNextQuestionIdInSameBranchAsync(long currentId);
        Task<ApiResponse> ValidateQuestionsByQuestionCodes(AddMultipleQuestionsRequestDto addMultipleQuestionsRequestDto);
        Task<ApiResponse> GetQuestionsByItemBankAndType(MixedSelectedQuestionsNodeDto autoSelectedQuestionsNodeDto);
        Task<ApiResponse> AddOrUpdateSegmentQuestionAsync(List<SegmentQuestionDto> segmentQuestionDto);
        Task<List<SegmentQuestionDto>> GetSegmentQuestionByMetaDataIdAsync(long parentId, long languageId);
        Task<ApiResponse> DeleteSegmentQuestionLanguageVariantAsync(long segmentQuestionMetadataId, long segmentQuestionDetailsId);
        Task<ApiResponse> AddOrUpdateMatchingPairsQuestionAsync(AddOrUpdateMatchingPairsRequestDto addOrUpdateMatchingPairsRequestDto);
        Task<ApiResponse> GetMatchingPairsQuestionAsync(long metadataParentId, long? languageId);
        Task<CustomTableData<QuestionAnalyticsIndicatorDto>> GetQuestionIndicatorDataAsync(PaginationSearchModel pagination);
        Task<ApiResponse> ExportQuestionIndicatorsAsync(QuestionIndicatorExportRequestDto request);
        Task<ApiResponse> AddOrUpdateMatchingPairsWithDragDropQuestionAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto request);
        Task<ApiResponse> GetMatchingPairsWithDragDropQuestionAsync(long metadataParentId, long? languageId);
        Task<CustomTableData<FilteredQuestionsDto>> GetFilteredQuestionsAsync(PaginationSearchModel pagination);
        Task<List<string>> GetQuestionAuthorsAsync();
        Task<CustomTableData<StandaloneQuestionsResponseDto>> GetAllStandaloneQuestionAsync(PaginationSearchModel paginationSearch);
        Task<ApiResponse> ValidateStandaloneQuestionsCodes(UploadFreeQuestions requestDto);
        Task<ApiResponse> AddTemplateAIQuestionAsync(AIQuestionTemplateCreationDto dto);
    }
}
