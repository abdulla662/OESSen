using OES.Helper.Dtos.AIQuestionGenerator.Response;
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
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IQuestionService
    {
        Task<ApiResponse> AddQuestionMetadataAsync(QuestionMetadataAdditionOrUpdateDto questionMetaData);
        Task<ApiResponse> AddQuestionTemplateAsync(QuestionCreationTemplateDto questionTemplateDto);
        Task<ApiResponse> AddNewQuestionLanguageDetails(QuestionDetailsDto questionDetailsDto);
        Task<ApiResponse> DeleteQuestionTemplateAsync(long templateId);
        Task<ApiResponse> AddComprehensionSubQuestionAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto);
        Task<ApiResponse> AddComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto);
        Task<ApiResponse> GetAllQuestionLanguage(PaginationSearchModel pagination, long QuestionMetaDataId = 0);
        Task<ApiResponse> EditQuestionMetadataAsync(QuestionMetadataAdditionOrUpdateDto questionMetaData);
        Task<ApiResponse> GetAllQuestionAsync(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetAllQuestionFromItemBankPoint(long paperId, PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetAllQuestionForPendingPage(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetAllQuestionForPendingOnlyPage(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> EditQuestionDetails(QuestionDetailsDto questionDetailsDto);
        Task<ApiResponse> GetQuestionDetailsByIdAsync(long questionDetailsId);
        Task<ApiResponse> GetQuestionMetadataByIdAsync(long questionMetadataId);
        Task<ApiResponse> DeleteQuestionDetails(long questionDetailsId);
        Task<ApiResponse> DeleteQuestionMetaData(long questionMetadataId);
        Task<ApiResponse> GetQuestionByMetaDataIdAndLanguageId(long metaDataId, long languageId);
        Task<ApiResponse> GetComprehensionSubQuestionsAsync(long rootComprehensionQuestionMetadataId, long languageId);
        Task<ApiResponse> UpdateComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto);
        Task<ApiResponse> DeleteComprehensionSubQuestionLanguageVariantAsync(long subQuestionMetadataId, long subQuestionDetailsId);
        Task<ApiResponse> GetAllQuestionTemplatesAsync(PaginationSearchModel paginationSearch, bool isFromQuestionAI);
        Task<ApiResponse> GetQuestionTemplateByIdAsync(long questionTemplateId);
        Task<ApiResponse> GetAllQuestionByItemBankId(List<long> itemBankIds);
        Task<ApiResponse> GetAllQuestionsByIds(List<long> questionIds);
        Task<ApiResponse> GetQuestionTypeCountsByItemBankId(long itemBankId);
        Task<ApiResponse> GetAllQuestionByBlockId(long blockId, PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetAllApprovedQuestionsForBlocksAsync(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> ChangeQuestionCreationStatusAsync(UpdateQuestionCreationStatusRequestDto updateQuestionCreationStatusRequestDto);
        Task<ApiResponse> ChangeQuestionsCreationStatusAsync(UpdateQuestionsCreationStatusRequestDto updateQuestionsCreationStatusRequestDto);
        Task<ApiResponse> GetNextQuestionForQualityCheckAsync(long currentId);
        Task<ApiResponse> GetPreviousQuestionForQualityCheckAsync(long currentId);
        Task<ApiResponse> GetNextQuestionIdInSameBranchAsync(long currentId);
        Task<ApiResponse> ValidateQuestionsByQuestionCodesAsync(AddMultipleQuestionsRequestDto addMultipleQuestionsRequestDto);
        Task<ApiResponse> GetQuestionsByItemBankAndType(MixedSelectedQuestionsNodeDto autoSelectedQuestionsNodeDto);
        Task<ApiResponse> GetSegmentQuestionByMetaDataIdAsync(long parentId, long languageId);
        Task<ApiResponse> AddOrUpdateSegmentQuestionAsync(List<SegmentQuestionDto> segmentQuestionDto);
        Task<ApiResponse> DeleteSegmentQuestionLanguageVariantAsync(long segmentQuestionMetadataId, long segmentQuestionDetailsId);
        Task<ApiResponse> AddOrUpdateMatchingPairsQuestionAsync(AddOrUpdateMatchingPairsRequestDto addOrUpdateMatchingPairsRequestDto);
        Task<ApiResponse> GetMatchingPairsQuestionAsync(long metadataParentId, long? languageId);
        Task<ApiResponse> AddOrUpdateMatchingPairsWithDragDropQuestionAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto request);
        Task<ApiResponse> GetMatchingPairsWithDragDropQuestionAsync(long metadataParentId, long? languageId);
        Task<ApiResponse> GetAllQuestionWithGroupsAsync();
        Task<ApiResponse> ValidateStandaloneQuestionsAsync(UploadFreeQuestions request);
        Task<ApiResponse> GetQuestionGroupsAsync(long questionId);
        Task<ApiResponse> GetQuestionAnalyticsIndicatorsAsync(PaginationSearchModel searchModel);
        Task<ApiResponse> ExportQuestionIndicatorsAsync(QuestionIndicatorExportRequestDto request);
        Task<ApiResponse> GetFilteredQuestionsAsync(PaginationSearchModel paginationSearchModel);
        Task<ExcelFileResult> ExportFilteredQuestionsToExcelAsync(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> GetQuestionAuthorsAsync();
        Task<ApiResponse> GetStandaloneQuestionMetadataAsync(PaginationSearchModel paginationSearchModel);
        Task<ApiResponse> SaveAIGeneratedQuestionsAsync(IReadOnlyList<AIQuestionMetadataDto> questions);
        Task<ApiResponse> AddTemplateAIQuestionAsync(AIQuestionTemplateCreationDto dto);
    }
}
