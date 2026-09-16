using OES.Helper.Dtos.FlattenedTree.Responses;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Paper
{
    public interface IBlazPaperService
    {
        // GET METHODS

        Task<CustomTableData<UserPapersListDto>> GetAllUserPapersAsync(PaginationSearchModel paginationSearch);

        Task<List<UserPaperForSchedule>> GetAllUserPapersAsync(long scheduleMetadataId);

        Task<List<GetUserPapersDto>> GetPapersWithoutEquationTemplateAsync();

        Task<List<GetUserPapersDto>> GetPapersWithEquationTemplateAsync();

        Task<CustomTableData<ManualQuestionsPaginationResponseDto>> GetAvailableManualQuestionsForPaperAsync(long paperId, PaginationSearchModel model);

        Task<CustomTableData<PaperMetadataTemplateDto>> GetPaperPaperMetadataTemplatesAsync(PaginationSearchModel paginationSearch);

        Task<PaperDataViewResponseDto> GetPaperDataForViewByIdAsync(long id);

        Task<ApiResponse> GetPaperMetadataTemplateByIdAsync(long id);

        Task<PaperDurationResponseDto> GetPaperDurationByIdAsync(long id);

        Task<ApiResponse> GetPaperMetaDataAsync(long paperId);

        Task<ManualQuestionsResultDto> GetManuallySelectedQuestionsForUpdateAsync(long paperId, long? formId);

        Task<GetPaperManualSectionsWithQuestionsResponseDto> GetPaperManualSectionsWithQuestionsAsync(long paperId);

        Task<GetPaperAutoSectionsWithQuestionsDistributionsResponseDto> GetPaperAutoSectionsWithTheirQuestionsDistributionsAsync(long paperId);

        Task<List<AutoSelectedQuestionsResponseDto>> GetAutoSelectedQuestionsForMarkingSchemeAsync(long paperId);

        Task<GetPaperItemBanksResponseDto> GetItemBanksFlattenedTreeNodesAsync(long paperId);

        Task<List<ItemBanksFromItemBankPointResponseDto>> GetAllItemBanksFromItemBankPointsAsync(long paperId);

        Task<CollectivePaperFormSectionQuestionResponseDto> GetManuallySelectedQuestionsWithFormsForMarkingSchemeAsync(long paperId);

        Task<ApiResponse> GetCurrentPaperCreationStatusAsync(long paperId);

        Task<ApiResponse> CopyPaperAsync(long paperId);

        Task<List<GetOESGroupDto>> GetAllPaperGroupsCreatedByUser();

        Task<PaperGroupsDto> GetPaperGroupsAsync(long paperId);

        Task<bool> IsPaperAssignedToScheduleAsync(long paperId);


        // ADD METHODS

        Task<ApiResponse> AddPaperMetaDataAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataCreationRequestDto);

        Task<ApiResponse> AddManualFormUsingExcelSheetAsync(long paperId, AddOrUpdatePaperMetadataRequestDto addOrUpdatePaperMetadataRequestDto);

        Task<ApiResponse> AddOrUpdateManualQuestionSectioningAsync(long paperId, ManualSectioningRequestDto manualSectioningRequestDto);

        Task<ApiResponse> AddManuallySelectedQuestionsAsync(long paperId, AddOrUpdateManualQuestionsRequestDto manualSave);

        Task<ApiResponse> AddOrUpdateSectionsDistributionsForAutoAsync(AddOrUpdateAutoSectionsDistributionsRequestDto addOrUpdateAutoSectionsDistributionsRequestDto);

        Task<ApiResponse> SaveTemplatePaperMetadataAsync(PaperMetadataSavingRequestDto PaperTemplateDto);


        // UPDATE METHODS

        Task<ApiResponse> UpdatePaperMetadataAsync(long paperId, AddOrUpdatePaperMetadataRequestDto editPaperMetadataRequestDto);

        Task<ApiResponse> UpdatePaperCreationStatusAsync(UpdatePaperCreationStatusRequestDto updatePaperCreationStatusRequestDto);

        Task<ApiResponse> UpdateManuallySelectedQuestionsAsync(AddOrUpdateManualQuestionsRequestDto manualSave, long paperId, long? formId);

        Task<ApiResponse> SuspendPaperAsync(long paperId);


        // DELETE METHODS

        Task<ApiResponse> DeletePaperMetadataTemplateAsync(long templateId);

        Task<ApiResponse> DeletePaperAsync(long id);


        // LOCK METHODS

        Task<ApiResponse> AcquirePaperLockAsync(PaperLockRequestDto paperLockRequestDto);

        Task<ApiResponse> RenewPaperLockAsync(PaperLockRequestDto paperLockRequestDto);

        Task<ApiResponse> ReleasePaperLockAsync(PaperLockRequestDto paperLockRequestDto);
    }
}