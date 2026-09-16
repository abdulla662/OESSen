using Newtonsoft.Json;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.Paper.ImportQuestionsRequestDto;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.Navigation;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionIndicators;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.QuestionTemplateDto;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;
using System.Net.Http.Headers;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OES.Blazor.Services.Implementation.Question
{
    public class BlazQuestionService : IBlazQuestionService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IBlazGetCustomTableData<QuestionLanguageDTO> _blazGetCustomTableData;
        private readonly IBlazGetCustomTableData<QuestionMetadataPaginationDto> _blazGetCustomTableDataQD;
        private readonly IBlazGetCustomTableData<ApprovedQuestionsPaginationDto> _blazGetCustomTableDataAQD;
        private readonly IBlazGetCustomTableData<BlockQuestionsDetailsPaginationDto> _blazGetCustomTableDataBQD;
        private readonly IBlazGetCustomTableData<PendingQuestionPaginationDto> _blazGetCustomTableDataPQD;
        private readonly IBlazGetCustomTableData<ManualQuestionsPaginationResponseDto> _blazGetCustomTableDataITP;
        private readonly IBlazGetCustomTableData<QuestionTemplateDto> _blazGetQuestionTemplateDto;
        private readonly IBlazGetCustomTableData<FilteredQuestionsDto> _blazGetCustomTableDataQAudit;
        private readonly IBlazGetCustomTableData<StandaloneQuestionsResponseDto> _blazGetCustomTableDataStandaloneQuestions;

        public BlazQuestionService(IHttpClientHelper httpClientHelper,
                                   IBlazGetCustomTableData<QuestionLanguageDTO> blazGetCustomTable,
                                   IBlazGetCustomTableData<QuestionMetadataPaginationDto> blazGetCustomTableDataQD,
                                   IBlazGetCustomTableData<ApprovedQuestionsPaginationDto> blazGetCustomTableDataAQD,
                                   IBlazGetCustomTableData<BlockQuestionsDetailsPaginationDto> blazGetCustomTableDataBQD,
                                   IBlazGetCustomTableData<PendingQuestionPaginationDto> blazGetCustomTableDataPQD,
                                   IBlazGetCustomTableData<QuestionTemplateDto> blazGetQuestionTemplateDto,
                                   IBlazGetCustomTableData<ManualQuestionsPaginationResponseDto> blazGetCustomTableDataITP,
                                   IBlazGetCustomTableData<FilteredQuestionsDto> blazGetCustomTableDataQAudit,
                                   IBlazGetCustomTableData<StandaloneQuestionsResponseDto> blazGetCustomTableStandaloneQuestions
        )
        {
            _httpClientHelper = httpClientHelper;
            _blazGetCustomTableData = blazGetCustomTable;
            _blazGetCustomTableDataQD = blazGetCustomTableDataQD;
            _blazGetCustomTableDataAQD = blazGetCustomTableDataAQD;
            _blazGetCustomTableDataBQD = blazGetCustomTableDataBQD;
            _blazGetCustomTableDataPQD = blazGetCustomTableDataPQD;
            _blazGetQuestionTemplateDto = blazGetQuestionTemplateDto;
            _blazGetCustomTableDataITP = blazGetCustomTableDataITP;
            _blazGetCustomTableDataQAudit = blazGetCustomTableDataQAudit;
            _blazGetCustomTableDataStandaloneQuestions = blazGetCustomTableStandaloneQuestions;
        }

        public async Task<ApiResponse> AddQuestionDetails(QuestionDetailsDto questionDetailsDto)
        {
            var Response = await _httpClientHelper.PostAsync(questionDetailsDto, "api/Question/AddNewQuestionDetails");

            return Response;
        }

        public async Task<ApiResponse> AddTemplateQuestionAsync(QuestionCreationTemplateDto questionTemplateDto)
        {
            var Response = await _httpClientHelper.PostAsync(questionTemplateDto, "api/Question/AddTemplateQuestion");

            return Response;
        }

        public async Task<ApiResponse> AddTemplateAIQuestionAsync(AIQuestionTemplateCreationDto dto)
        {
            var response = await _httpClientHelper.PostAsync(dto, "api/Question/AddTemplateAIQuestion");

            return response;
        }

        public async Task<QuestionGroupsDto> GetQuestionGroupsAsync(long questionId)
        {
            var response = await _httpClientHelper.GetAsync<QuestionGroupsDto>($"api/Question/GetQuestionGroups?questionId={questionId}");

            return (QuestionGroupsDto)response.Data;
        }

        public async Task<ApiResponse> AddQuestionMetaData(QuestionMetadataAdditionOrUpdateDto questionMetaData)
        {
            var Response = await _httpClientHelper.PostAsync(questionMetaData, "api/Question/AddNewQuestion");

            return Response;
        }

        public async Task<CustomTableData<QuestionLanguageDTO>> GetAllQuestionLanguage(PaginationSearchModel pagination, long QuestionMetaDataId)
        {
            var x = await _blazGetCustomTableData.GetCustomTableData(pagination, $"api/Question/GetAllQuestionLanguage?QuestionMetaDataId={QuestionMetaDataId}");

            return x;
        }

        public async Task<ApiResponse> EditQuestionMetaData(QuestionMetadataAdditionOrUpdateDto questionMetaData)
        {
            var response = await _httpClientHelper.PostAsync(questionMetaData, "api/Question/EditQuestion");

            return response;
        }

        public async Task<ApiResponse> EditQuestionDetailsAsync(QuestionDetailsDto questionDetailsDto)
        {
            var response = await _httpClientHelper.PostAsync(questionDetailsDto, "api/Question/EditQuestionDetails");

            return response;
        }

        public async Task<ApiResponse> UpdateComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto subQuestionLanguageVariantDto)
        {
            var response = await _httpClientHelper.PostAsync(subQuestionLanguageVariantDto, "api/Question/UpdateComprehensionSubQuestionLanguageVariant");

            return response;
        }

        public async Task<ApiResponse> ChangeQuestionCreationStatusAsync(UpdateQuestionCreationStatusRequestDto updateQuestionCreationStatusRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(updateQuestionCreationStatusRequestDto, "api/Question/ChangeQuestionCreationStatus");

            return response;
        }

        public async Task<ApiResponse> ChangeQuestionsCreationStatusAsync(UpdateQuestionsCreationStatusRequestDto updateQuestionsCreationStatusRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(updateQuestionsCreationStatusRequestDto, "api/Question/ChangeQuestionsCreationStatus");

            return response;
        }

        public async Task<ApiResponse> AddComprehensionSubQuestionAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            var response = await _httpClientHelper.PostAsync(comprehensionSubQuestionDto, "api/Question/AddComprehensionSubQuestion");

            return response;
        }

        public async Task<ApiResponse> AddComprehensionSubQuestionLanguageVariantAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto)
        {
            var response = await _httpClientHelper.PostAsync(comprehensionSubQuestionDto, "api/Question/AddComprehensionSubQuestionLanguageVariant");

            return response;
        }

        public async Task<CustomTableData<QuestionMetadataPaginationDto>> GetAllQuestionAsync(PaginationSearchModel paginationSearch)
        {
            var result = await _blazGetCustomTableDataQD.GetCustomTableData(paginationSearch, "api/Question/GetAllQuestion");

            return result;
        }

        public async Task<CustomTableData<StandaloneQuestionsResponseDto>> GetAllStandaloneQuestionAsync(PaginationSearchModel paginationSearch)
        {
            var result = await _blazGetCustomTableDataStandaloneQuestions.GetCustomTableData(paginationSearch, "api/Question/GetAllStandaloneQuestions");

            return result;
        }

        public async Task<List<GetOESGroupDto>> GetAllQuestionGroupsAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/Question/GetAllQuestionWithGroups");

            var data = (List<GetOESGroupDto>)response.Data;

            return data ?? [];
        }

        public async Task<CustomTableData<BlockQuestionsDetailsPaginationDto>> GetAllQuestionByBlockId(long blockId, PaginationSearchModel paginationSearch)
        {
            var result = await _blazGetCustomTableDataBQD.GetCustomTableData(paginationSearch, $"api/Question/GetAllQuestionByBlockId?blockId={blockId}");

            return result;
        }

        public async Task<CustomTableData<ApprovedQuestionsPaginationDto>> GetAllApprovedQuestionsForBlocksAsync(PaginationSearchModel paginationSearch)
        {
            var result = await _blazGetCustomTableDataAQD.GetCustomTableData(paginationSearch, "api/Question/GetAllApprovedQuestionsForBlocksAsync");

            return result;
        }

        public async Task<CustomTableData<ManualQuestionsPaginationResponseDto>> GetAllQuestionFormItemBankPoint(long paperId, PaginationSearchModel paginationSearch)
        {
            var result = await _blazGetCustomTableDataITP.GetCustomTableData(paginationSearch, $"api/Question/GetAllQuestionFromItemBankPoint?{nameof(paperId)}={paperId}");

            return result ?? new CustomTableData<ManualQuestionsPaginationResponseDto>([], 0);
        }

        public async Task<CustomTableData<PendingQuestionPaginationDto>> GetAllQuestionForPendingPage(PaginationSearchModel paginationSearch)
        {
            var result = await _blazGetCustomTableDataPQD.GetCustomTableData(paginationSearch, "api/Question/GetAllQuestionForPendingPage");

            return result;
        }

        public async Task<ApiResponse> GetQuestionByMetaDataIdAndLanguageId(long metaDataId, long languageId)
        {
            var response = await _httpClientHelper.GetAsync<QuestionDataDto>($"api/Question/GetQuestionByMetaDataIdAndLanguageId?metaDataId={metaDataId}&languageId={languageId}");

            return response;
        }

        public async Task<ApiResponse> GetQuestionDetailsByIdAsync(long questionDetailsId)
        {
            var response = await _httpClientHelper.GetAsync<QuestionDetailsDto>($"api/Question/GetQuestionDetailsById?questionDetailsId={questionDetailsId}");

            return response;
        }

        public async Task<ApiResponse> GetQuestionMetadataByIdAsync(long questionMetadataId)
        {
            var response = await _httpClientHelper.GetAsync<QuestionMetadataRetrievalDto>($"api/Question/GetQuestionMetadataById?questionMetadataId={questionMetadataId}");

            return response;
        }

        public async Task<ApiResponse> GetComprehensionSubQuestionsAsync(long rootComprehensionQuestionMetadataId, long languageId)
        {
            var response = await _httpClientHelper.GetAsync<List<SubQuestionDetailsDto>>($"api/Question/GetComprehensionSubQuestions?rootComprehensionQuestionMetadataId={rootComprehensionQuestionMetadataId}&languageId={languageId}");

            return response;
        }

        public async Task<ApiResponse> DeleteQuestionDetailsByIdAsync(long questionDetailsId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/Question/DeleteQuestionDetails?questionDetailsId={questionDetailsId}");

            return response;
        }

        public async Task<ApiResponse> DeleteQuestionMetadataByIdAsync(long questionMetadataId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/Question/DeleteQuestionMetadata?questionMetadataId={questionMetadataId}");

            return response;
        }

        public async Task<ApiResponse> DeleteComprehensionSubQuestionLanguageVariantAsync(long subQuestionMetadataId, long subQuestionDetailsId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/Question/DeleteComprehensionSubQuestionLanguageVariant?subQuestionMetadataId={subQuestionMetadataId}&&subQuestionDetailsId={subQuestionDetailsId}");

            return response;
        }

        public async Task<CustomTableData<QuestionTemplateDto>> GetAllQuestionTemplateAsync(QuestionPaginationSearchRequest questionPaginationSearchRequest)
        {
            var url = $"api/Question/GetAllQuestionTemplates?isFromQuestionAI={questionPaginationSearchRequest.IsFromQuestionAI}";

            var result = await _blazGetQuestionTemplateDto.GetCustomTableData(questionPaginationSearchRequest.PaginationSearch, url);

            return result;
        }

        public async Task<ApiResponse> GetQuestionTemplateById(long questionTemplateId)
        {
            var result = await _httpClientHelper.GetAsync<GetQuestionTemplateResponseDto>($"api/Question/GetQuestionTemplateById?questionTemplateId={questionTemplateId}");
            return result;
        }

        public async Task<CustomTableData<PendingQuestionPaginationDto>> GetAllQuestionForPendingOnlyPage(PaginationSearchModel paginationSearch)
        {
            var response = await _blazGetCustomTableDataPQD.GetCustomTableData(paginationSearch, "api/Question/GetAllQuestionForPendingOnlyPage");

            return response;
        }

        public async Task<ApiResponse> BypassQuestionsAsync(BypassQuestionsDto bypassQuestionsDto)
        {
            var response = await _httpClientHelper.PostAsync(bypassQuestionsDto, "api/QComment/BypassQuestions");

            return response;
        }

        public async Task<long?> GetNextQuestionIdAsync(long currentId)
        {
            var resp = await _httpClientHelper.GetAsync<QuestionNavigationDto>($"api/Question/GetNextQuestionForQualityCheck?currentId={currentId}");

            var dto = resp?.Data as QuestionNavigationDto;

            return dto?.TargetId;
        }

        public async Task<long?> GetPreviousQuestionIdAsync(long currentId)
        {
            var resp = await _httpClientHelper.GetAsync<QuestionNavigationDto>($"api/Question/GetPreviousQuestionForQualityCheck?currentId={currentId}");

            var dto = resp?.Data as QuestionNavigationDto;

            return dto?.TargetId;
        }

        public async Task<long?> GetNextQuestionIdInSameBranchAsync(long currentId)
        {
            var resp = await _httpClientHelper.GetAsync<QuestionNavigationDto>($"api/Question/GetNextQuestionIdInSameBranch?currentId={currentId}");

            var data = resp?.Data as QuestionNavigationDto;

            return data.TargetId;
        }

        public async Task<ApiResponse> ValidateQuestionsByQuestionCodes(AddMultipleQuestionsRequestDto addMultipleQuestionsRequestDto)
        {
            var response = await _httpClientHelper.PostAsync(addMultipleQuestionsRequestDto, "api/Question/ValidateQuestionsByQuestionCodes");

            return response;
        }

        public async Task<ApiResponse> GetQuestionsByItemBankAndType(MixedSelectedQuestionsNodeDto autoSelectedQuestionsNodeDto)
        {
            return await _httpClientHelper.PostAsync(autoSelectedQuestionsNodeDto, "api/Question/GetQuestionsByItemBankAndType");
        }

        public async Task<ApiResponse> DeleteQuestionTemplateAsync(long templateId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/Question/DeleteQuestionTemplate?{nameof(templateId)}={templateId}");

            return response;
        }

        public async Task<ApiResponse> AddOrUpdateSegmentQuestionAsync(List<SegmentQuestionDto> segmentQuestionDto)
        {
            var response = await _httpClientHelper.PostAsync(segmentQuestionDto, "api/Question/AddOrUpdateSegmentQuestion");

            return response;
        }

        public async Task<List<SegmentQuestionDto>> GetSegmentQuestionByMetaDataIdAsync(long parentId, long languageId)
        {
            var response = await _httpClientHelper.GetAsync<List<SegmentQuestionDto>>($"api/Question/GetSegmentQuestionByMetaDataId?{nameof(parentId)}={parentId}&{nameof(languageId)}={languageId}");

            var dtos = response?.Data as List<SegmentQuestionDto>;

            return dtos;
        }

        public async Task<ApiResponse> DeleteSegmentQuestionLanguageVariantAsync(long segmentQuestionMetadataId, long segmentQuestionDetailsId)
        {
            var response = await _httpClientHelper.DeleteAsync($"api/Question/DeleteSegmentQuestionLanguageVariant?{nameof(segmentQuestionMetadataId)}={segmentQuestionMetadataId}&{nameof(segmentQuestionDetailsId)}={segmentQuestionDetailsId}");

            return response;
        }

        public async Task<ApiResponse> AddOrUpdateMatchingPairsQuestionAsync(AddOrUpdateMatchingPairsRequestDto addOrUpdateMatchingPairsRequestDto)
        {
            return await _httpClientHelper.PostAsync(addOrUpdateMatchingPairsRequestDto, "api/Question/AddOrUpdateMatchingPairsQuestion");
        }

        public async Task<ApiResponse> GetMatchingPairsQuestionAsync(long metadataParentId, long? languageId)
        {
            var baseRelativeUrl = $"api/Question/GetMatchingPairsQuestion?{nameof(metadataParentId)}={metadataParentId}";

            if (languageId.HasValue)
            {
                baseRelativeUrl += $"&{nameof(languageId)}={languageId.Value}";
            }

            var result = await _httpClientHelper.GetAsync<GetMatchingPairsResponseDto>(baseRelativeUrl);

            return result;
        }

        public async Task<CustomTableData<QuestionAnalyticsIndicatorDto>> GetQuestionIndicatorDataAsync(PaginationSearchModel pagination)
        {
            var result = await _httpClientHelper.PostAsync(pagination, "api/Question/GetQuestionIndicators");

            var data = JsonConvert.DeserializeObject<CustomTableData<QuestionAnalyticsIndicatorDto>>(
                result.Data.ToString()
            );

            return data;
        }

        public async Task<ApiResponse> ExportQuestionIndicatorsAsync(QuestionIndicatorExportRequestDto request)
        {
            return await _httpClientHelper.PostAsync(request, "api/Question/ExportQuestionIndicators");
        }

        public async Task<ApiResponse> AddOrUpdateMatchingPairsWithDragDropQuestionAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto AddOrUpdateMatchingPairsWithDragDropRequestDto)
        {
            return await _httpClientHelper.PostAsync(AddOrUpdateMatchingPairsWithDragDropRequestDto, "api/Question/AddOrUpdateMatchingPairsWithDragDropQuestion");
        }

        public async Task<ApiResponse> GetMatchingPairsWithDragDropQuestionAsync(long metadataParentId, long? languageId)
        {
            var baseRelativeUrl = $"api/Question/GetMatchingPairsWithDragDropQuestion?{nameof(metadataParentId)}={metadataParentId}";

            if (languageId.HasValue)
            {
                baseRelativeUrl += $"&{nameof(languageId)}={languageId.Value}";
            }

            var result = await _httpClientHelper.GetAsync<GetMatchingPairsWithDragDropResponseDto>(baseRelativeUrl);

            return result;
        }

        public async Task<ApiResponse> GetQuestionFilteredListAsync(PaginationSearchModel pagination)
        {
            return await _httpClientHelper.PostAsync(pagination, "api/Question/GetAllQuestion");
        }

        public async Task<CustomTableData<FilteredQuestionsDto>> GetFilteredQuestionsAsync(PaginationSearchModel pagination)
        {
            return await _blazGetCustomTableDataQAudit.GetCustomTableData(pagination, "api/Question/GetFilteredQuestions");
        }

        public async Task<List<string>> GetQuestionAuthorsAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<string>>("api/Question/GetQuestionAuthors");
            return (List<string>)response?.Data ?? [];
        }

        public async Task<ApiResponse> ValidateStandaloneQuestionsCodes(UploadFreeQuestions requestDto)
        {
            using var formData = new MultipartFormDataContent();

            using var streamContent = new StreamContent(requestDto.File.OpenReadStream());

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(requestDto.File.ContentType);

            formData.Add(streamContent, MiscConstants.File, requestDto.File.FileName);

            var response = await _httpClientHelper._httpClient.PostAsync("api/Question/ValidateStandaloneQuestions", formData);

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ApiResponse>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
