using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Common;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.FlattenedTree.Responses;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.Enums;
using OES.Helper.General;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.Paper
{
    public class BlazPaperService(IHttpClientHelper _httpClient, IBlazGetCustomTableData<UserPapersListDto> _blazGetCustomTableData, IBlazGetCustomTableData<PaperMetadataTemplateDto> _blazGetPaperMetadataTemplateCustomTableData) : IBlazPaperService
    {
        // GET METHODS

        public async Task<ApiResponse> GetPaperMetaDataAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<GetPaperMetadataResponseDto>($"api/Paper/GetPaperMetaData?{nameof(paperId)}={paperId}");

            return response;
        }

        public async Task<List<UserPaperForSchedule>> GetAllUserPapersAsync(long scheduleMetadataId)
        {
            var response = await _httpClient.GetAsync<List<UserPaperForSchedule>>($"api/Paper/GetAllUserPapers?{nameof(scheduleMetadataId)}={scheduleMetadataId}");

            return (List<UserPaperForSchedule>)response.Data ?? [];
        }

        public async Task<List<GetUserPapersDto>> GetPapersWithoutEquationTemplateAsync()
        {
            var response = await _httpClient.GetAsync<List<GetUserPapersDto>>($"api/Paper/GetPapersWithoutEquationTemplateAsync");

            return (List<GetUserPapersDto>)response.Data ?? [];
        }

        public async Task<List<GetUserPapersDto>> GetPapersWithEquationTemplateAsync()
        {
            var response = await _httpClient.GetAsync<List<GetUserPapersDto>>("api/Paper/GetPapersWithEquationTemplateAsync");

            return (List<GetUserPapersDto>)response.Data ?? [];
        }

        public async Task<CustomTableData<ManualQuestionsPaginationResponseDto>> GetAvailableManualQuestionsForPaperAsync(long paperId, PaginationSearchModel model)
        {
            var response = await _httpClient.PostAsync(
                model,
                $"api/Paper/GetAvailableManualQuestionsForPaper?paperId={paperId}"
            );

            if (response?.Data == null)
                return new CustomTableData<ManualQuestionsPaginationResponseDto>([], 0);

            var json = response.Data.ToString();

            var result = JsonSerializer.Deserialize<CustomTableData<ManualQuestionsPaginationResponseDto>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return result ?? new CustomTableData<ManualQuestionsPaginationResponseDto>([], 0);
        }

        public async Task<CustomTableData<PaperMetadataTemplateDto>> GetPaperPaperMetadataTemplatesAsync(PaginationSearchModel paginationSearch)
        {
            var response = await _blazGetPaperMetadataTemplateCustomTableData.GetCustomTableData(paginationSearch, "api/Paper/GetPaperMetadataTemplates");

            return response;
        }

        public async Task<PaperDataViewResponseDto> GetPaperDataForViewByIdAsync(long id)
        {
            var response = await _httpClient.GetAsync<PaperDataViewResponseDto>($"api/Paper/GetPaperDataForViewById?id={id}");

            return (PaperDataViewResponseDto)response.Data;
        }

        public async Task<ApiResponse> GetPaperMetadataTemplateByIdAsync(long id)
        {
            var response = await _httpClient.GetAsync<AddOrUpdatePaperMetadataRequestDto>($"api/Paper/GetPaperMetadataTemplateById?id={id}");

            return response;
        }

        public async Task<PaperDurationResponseDto> GetPaperDurationByIdAsync(long id)
        {
            var response = await _httpClient.GetAsync<PaperDurationResponseDto>($"api/Paper/GetPaperDurationById?id={id}");

            return (PaperDurationResponseDto)response.Data;
        }

        public async Task<List<AutoSelectedQuestionsResponseDto>> GetAutoSelectedQuestionsForMarkingSchemeAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<List<AutoSelectedQuestionsResponseDto>>($"api/Paper/GetAutoSelectedQuestionsForMarkingScheme?{nameof(paperId)}={paperId}");

            return (List<AutoSelectedQuestionsResponseDto>)response.Data ?? [];
        }

        public async Task<CustomTableData<UserPapersListDto>> GetAllUserPapersAsync(PaginationSearchModel paginationSearch)
        {
            var response = await _blazGetCustomTableData.GetCustomTableData(paginationSearch, "api/Paper/GetAllUserPapers");

            return response;
        }

        public async Task<GetPaperItemBanksResponseDto> GetItemBanksFlattenedTreeNodesAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<GetPaperItemBanksResponseDto>($"api/Paper/GetItemBanksFlattenedTreeNodes?{nameof(paperId)}={paperId}");

            return (GetPaperItemBanksResponseDto)response.Data;
        }

        public async Task<List<ItemBanksFromItemBankPointResponseDto>> GetAllItemBanksFromItemBankPointsAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<List<ItemBanksFromItemBankPointResponseDto>>($"api/Paper/GetAllItemBanksFromItemBankPoints?{nameof(paperId)}={paperId}");

            return (List<ItemBanksFromItemBankPointResponseDto>)response.Data;
        }

        public async Task<CollectivePaperFormSectionQuestionResponseDto> GetManuallySelectedQuestionsWithFormsForMarkingSchemeAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<CollectivePaperFormSectionQuestionResponseDto>($"api/Paper/GetManuallySelectedQuestionsWithFormsForMarkingScheme?{nameof(paperId)}={paperId}");

            return (CollectivePaperFormSectionQuestionResponseDto)response.Data ?? new([], [], []);
        }

        public async Task<GetPaperManualSectionsWithQuestionsResponseDto> GetPaperManualSectionsWithQuestionsAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<GetPaperManualSectionsWithQuestionsResponseDto>($"api/Paper/GetPaperManualSectionsWithQuestions?{nameof(paperId)}={paperId}");

            return (GetPaperManualSectionsWithQuestionsResponseDto)response.Data ?? new([], [], [], []);
        }

        public async Task<GetPaperAutoSectionsWithQuestionsDistributionsResponseDto> GetPaperAutoSectionsWithTheirQuestionsDistributionsAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<GetPaperAutoSectionsWithQuestionsDistributionsResponseDto>($"api/Paper/GetPaperAutoSectionsWithTheirQuestionsDistributions?{nameof(paperId)}={paperId}");

            return (GetPaperAutoSectionsWithQuestionsDistributionsResponseDto)response.Data ?? new([], []);
        }

        public async Task<ManualQuestionsResultDto> GetManuallySelectedQuestionsForUpdateAsync(long paperId, long? formId)
        {
            var url = $"api/Paper/GetManuallySelectedQuestionsForUpdate?{nameof(paperId)}={paperId}";

            if (formId.HasValue)
                url += $"&{nameof(formId)}={formId.Value}";

            var response = await _httpClient.GetAsync<ManualQuestionsResultDto>(url);

            return (ManualQuestionsResultDto)response.Data ?? new ManualQuestionsResultDto();
        }

        public async Task<ApiResponse> GetCurrentPaperCreationStatusAsync(long paperId)
        {
            return await _httpClient.GetAsync<GetCurrentPaperCreationStatusResponseDto>($"api/Paper/GetCurrentPaperCreationStatus?{nameof(paperId)}={paperId}");
        }

        public async Task<ApiResponse> CopyPaperAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<object>($"api/Paper/CopyPaperAsync?{nameof(paperId)}={paperId}");

            return response;
        }

        public async Task<List<GetOESGroupDto>> GetAllPaperGroupsCreatedByUser()
        {
            var response = await _httpClient.GetAsync<List<GetOESGroupDto>>("api/Paper/GetAllPaperGroupsCreatedByUser");

            var data = (List<GetOESGroupDto>)response.Data;

            return data ?? [];
        }

        public async Task<PaperGroupsDto> GetPaperGroupsAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<PaperGroupsDto>($"api/Paper/GetPaperGroups?paperId={paperId}");

            return (PaperGroupsDto)response.Data;
        }

        public async Task<bool> IsPaperAssignedToScheduleAsync(long paperId)
        {
            var response = await _httpClient.GetAsync<object>($"api/Paper/IsPaperAssignedToSchedule?{nameof(paperId)}={paperId}");

            return response.CustomCodeStatus == CustomCodeStatus.Success;
        }


        // ADD METHODS

        public async Task<ApiResponse> AddPaperMetaDataAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataCreationRequestDto)
        {
            return await _httpClient.PostAsync(paperMetadataCreationRequestDto, "api/Paper/AddPaperMetaData");
        }

        public async Task<ApiResponse> AddManualFormUsingExcelSheetAsync(long paperId, AddOrUpdatePaperMetadataRequestDto addOrUpdatePaperMetadataRequestDto)
        {
            return await _httpClient.PostAsync(addOrUpdatePaperMetadataRequestDto, $"api/Paper/AddManualFormUsingExcelSheet?{nameof(paperId)}={paperId}");
        }

        public async Task<ApiResponse> AddManuallySelectedQuestionsAsync(long paperId, AddOrUpdateManualQuestionsRequestDto manualSave)
        {
            return await _httpClient.PostAsync(manualSave, $"api/Paper/AddManuallySelectedQuestions?{nameof(paperId)}={paperId}");
        }

        public async Task<ApiResponse> AddOrUpdateManualQuestionSectioningAsync(long paperId, ManualSectioningRequestDto manualSectioningRequestDto)
        {
            return await _httpClient.PostAsync(manualSectioningRequestDto, $"api/Paper/AddOrUpdateManualQuestionSectioning?{nameof(paperId)}={paperId}");
        }

        public async Task<ApiResponse> AddOrUpdateSectionsDistributionsForAutoAsync(AddOrUpdateAutoSectionsDistributionsRequestDto addOrUpdateAutoSectionsDistributionsRequestDto)
        {
            return await _httpClient.PostAsync(addOrUpdateAutoSectionsDistributionsRequestDto, "api/Paper/AddOrUpdateSectionsDistributionsForAuto");
        }

        public async Task<ApiResponse> SaveTemplatePaperMetadataAsync(PaperMetadataSavingRequestDto PaperTemplateDto)
        {
            var Response = await _httpClient.PostAsync(PaperTemplateDto, "api/Paper/SavePaperMetadataTemplate");

            return Response;
        }


        // UPDATE METHODS

        public async Task<ApiResponse> UpdatePaperMetadataAsync(long paperId, AddOrUpdatePaperMetadataRequestDto editPaperMetadataRequestDto)
        {
            return await _httpClient.PutAsync(editPaperMetadataRequestDto, $"api/Paper/UpdatePaperMetadata?{nameof(paperId)}={paperId}");
        }

        public async Task<ApiResponse> UpdatePaperCreationStatusAsync(UpdatePaperCreationStatusRequestDto updatePaperCreationStatusRequestDto)
        {
            return await _httpClient.PutAsync(updatePaperCreationStatusRequestDto, "api/Paper/UpdatePaperCreationStatus");
        }

        public async Task<ApiResponse> UpdateManuallySelectedQuestionsAsync(AddOrUpdateManualQuestionsRequestDto manualSave, long paperId, long? formId)
        {
            var url = $"api/Paper/UpdateManuallySelectedQuestions?{nameof(paperId)}={paperId}";

            if (formId.HasValue)
            {
                url += $"&{nameof(formId)}={formId.Value}";
            }

            return await _httpClient.PutAsync(manualSave, url);
        }

        public async Task<ApiResponse> SuspendPaperAsync(long paperId)
        {
            return await _httpClient.PostAsync(paperId, "api/Paper/SuspendPaper");
        }


        // DELETE METHODS

        public async Task<ApiResponse> DeletePaperMetadataTemplateAsync(long templateId)
        {
            var response = await _httpClient.DeleteAsync($"api/Paper/DeletePaperMetadataTemplate?{nameof(templateId)}={templateId}");

            return response;
        }

        public async Task<ApiResponse> DeletePaperAsync(long id)
        {
            return await _httpClient.DeleteAsync($"api/Paper/DeletePaper?{nameof(id)}={id}");
        }


        // LOCK METHODS

        public async Task<ApiResponse> AcquirePaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            return await _httpClient.PostAsync(paperLockRequestDto, "api/Paper/AcquirePaperLock");
        }

        public async Task<ApiResponse> RenewPaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            return await _httpClient.PutAsync(paperLockRequestDto, "api/Paper/RenewPaperLock");
        }

        public async Task<ApiResponse> ReleasePaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            return await _httpClient.PostAsync(paperLockRequestDto, "api/Paper/ReleasePaperLock");
        }
    }
}