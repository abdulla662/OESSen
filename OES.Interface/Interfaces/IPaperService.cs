using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.General;
using OES.Helper.Interfaces;

namespace OES.Interface.Interfaces
{
    public interface IPaperService
    {
        // GET METHODS

        Task<IApiResponse> GetAllUserPapersAsync(PaginationSearchModel pagination);

        Task<IApiResponse> GetAllUserPapersAsync(long scheduleMetadataId);

        Task<IApiResponse> GetAvailableManualQuestionsForPaperAsync(long paperId, PaginationSearchModel paginationSearchModel);

        Task<IApiResponse> GetPapersWithoutEquationTemplateAsync();

        Task<IApiResponse> GetPapersWithEquationTemplateAsync();

        Task<IApiResponse> GetPaperMetaDataAsync(long paperId);

        Task<IApiResponse> GetCurrentPaperCreationStatusAsync(long paperId);

        Task<IApiResponse> GetPaperDataForViewByIdAsync(long id);

        Task<IApiResponse> GetPaperDurationByIdAsync(long id);

        Task<IApiResponse> GetManuallySelectedQuestionsForUpdateAsync(long paperId, long? formId = null);

        Task<IApiResponse> GetAllPaperMetadataTemplatesAsync(PaginationSearchModel paginationSearchModel);

        Task<IApiResponse> GetPaperMetadataTemplateByIdAsync(long PaperMetadataTemplateId);

        Task<IApiResponse> SavePaperMetadataTemplateAsync(PaperMetadataSavingRequestDto PaperTemplateDto);

        Task<IApiResponse> GetManuallySelectedQuestionsWithFormsForMarkingSchemeAsync(long paperId);

        Task<ApiResponse> GetPaperManualSectionsWithQuestionsAsync(long paperId);

        Task<IApiResponse> GetPaperAutoSectionsWithTheirQuestionsDistributionsAsync(long paperId);

        Task<IApiResponse> GetItemBanksFlattenedTreeNodesAsync(long paperId);

        Task<IApiResponse> GetAutoSelectedQuestionsForMarkingSchemeAsync(long paperId);

        Task<IApiResponse> GetPaperDifficultyLevelsAsync(long paperId);

        Task<IApiResponse> GetAllItemBanksFromItemBankPointsAsync(long paperId);

        Task<IApiResponse> CopyPaperAsync(long paperId);

        Task<ApiResponse> GetAllPaperGroupsCreatedByUser();

        Task<IApiResponse> GetPaperGroupsAsync(long paperId);

        Task<ApiResponse> IsPaperAssignedToScheduleAsync(long paperId);


        // ADD METHODS

        Task<IApiResponse> AddPaperMetaDataAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataCreationRequestDto);

        Task<ApiResponse> AddManualFormUsingExcelSheetAsync(long paperId, AddOrUpdatePaperMetadataRequestDto addOrUpdatePaperMetadataRequestDto);

        Task<ApiResponse> AddManuallySelectedQuestionsAsync(long paperId, AddOrUpdateManualQuestionsRequestDto addManualQuestionsRequestDto, bool usesExcelQuestionsImport = false);

        Task<IApiResponse> AddOrUpdateManualQuestionSectioningAsync(long paperId, List<ManuallySelectedQuestionsResponseDto> manualQuestions, Dictionary<string, List<SectionRequestDto>> formSections, Dictionary<string, List<long>> formQuestionMap, FormMetadataDto formMetadata);

        Task<IApiResponse> AddOrUpdateSectionsDistributionsForAutoAsync(AddOrUpdateAutoSectionsDistributionsRequestDto addOrUpdateAutoSectionsDistributionsRequestDto);


        // UPDATE METHODS

        Task<IApiResponse> UpdatePaperMetadataAsync(long paperId, AddOrUpdatePaperMetadataRequestDto editPaperMetadataRequestDto);

        Task<IApiResponse> UpdatePaperCreationStatusAsync(UpdatePaperCreationStatusRequestDto updatePaperCreationStatusRequestDto);

        Task<IApiResponse> UpdateManuallySelectedQuestionsAsync(long paperId, long? formId, AddOrUpdateManualQuestionsRequestDto updateManualQuestionsRequestDto);


        // DELETE METHODS

        Task<ApiResponse> DeletePaperMetadataTemplateAsync(long templateId);

        Task<IApiResponse> DeletePaperAsync(long id);


        // LOCK METHODS

        Task<ApiResponse> AcquirePaperLockAsync(PaperLockRequestDto paperLockRequestDto);

        Task<ApiResponse> RenewPaperLockAsync(PaperLockRequestDto paperLockRequestDto);

        Task<ApiResponse> ReleasePaperLockAsync(PaperLockRequestDto paperLockRequestDto);
    }
}