using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.SectionDistributionDto;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class PaperController : OESBaseController
    {
        private readonly IPaperService _paperService;


        public PaperController(IPaperService paperService)
        {
            _paperService = paperService;
        }


        // GET ENDPOINTS

        [HttpPost("GetAllUserPapers")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllUserPapersAsync(PaginationSearchModel paginationModel)
        {
            return await _paperService.GetAllUserPapersAsync(paginationModel);
        }

        [HttpGet("GetAllUserPapers")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllUserPapersAsync(long scheduleMetadataId)
        {
            return await _paperService.GetAllUserPapersAsync(scheduleMetadataId);
        }

        [HttpGet("GetPapersWithoutEquationTemplateAsync")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPapersWithoutEquationTemplateAsync()
        {
            return await _paperService.GetPapersWithoutEquationTemplateAsync();
        }

        [HttpGet("GetPapersWithEquationTemplateAsync")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPapersWithEquationTemplateAsync()
        {
            return await _paperService.GetPapersWithEquationTemplateAsync();
        }

        [HttpGet("GetPaperMetaData")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperMetaDataAsync(long paperId)
        {
            return await _paperService.GetPaperMetaDataAsync(paperId);
        }

        [HttpPost("GetPaperMetadataTemplates")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperMetadataTemplatesAsync(PaginationSearchModel paginationSearchModel)
        {
            return await _paperService.GetAllPaperMetadataTemplatesAsync(paginationSearchModel);
        }

        [HttpGet("GetPaperDataForViewById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperDataForViewByIdAsync(long id)
        {
            return await _paperService.GetPaperDataForViewByIdAsync(id);
        }

        [HttpGet("GetPaperMetadataTemplateById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperMetadataTemplateByIdAsync(long id)
        {
            return await _paperService.GetPaperMetadataTemplateByIdAsync(id);
        }

        [HttpPost("GetAvailableManualQuestionsForPaper")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAvailableManualQuestionsForPaperAsync(
            long paperId,
            PaginationSearchModel paginationSearchModel
        )
        {
            return await _paperService.GetAvailableManualQuestionsForPaperAsync(paperId, paginationSearchModel);
        }

        [HttpGet("GetPaperDurationById")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperDurationByIdAsync(long id)
        {
            return await _paperService.GetPaperDurationByIdAsync(id);
        }

        [HttpGet("GetManuallySelectedQuestionsForUpdate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetManuallySelectedQuestionsForUpdateAsync(long paperId, long? formId = null)
        {
            return await _paperService.GetManuallySelectedQuestionsForUpdateAsync(paperId, formId);
        }

        [HttpGet("GetPaperManualSectionsWithQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperManualSectionsWithQuestionsAsync(long paperId)
        {
            return await _paperService.GetPaperManualSectionsWithQuestionsAsync(paperId);
        }

        [HttpGet("GetPaperAutoSectionsWithTheirQuestionsDistributions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperAutoSectionsWithTheirQuestionsDistributionsAsync(long paperId)
        {
            return await _paperService.GetPaperAutoSectionsWithTheirQuestionsDistributionsAsync(paperId);
        }

        [HttpGet("GetManuallySelectedQuestionsWithFormsForMarkingScheme")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetManuallySelectedQuestionsWithFormsForMarkingSchemeAsync(long paperId)
        {
            return await _paperService.GetManuallySelectedQuestionsWithFormsForMarkingSchemeAsync(paperId);
        }

        [HttpGet("GetAutoSelectedQuestionsForMarkingScheme")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAutoSelectedQuestionsForMarkingSchemeAsync(long paperId)
        {
            return await _paperService.GetAutoSelectedQuestionsForMarkingSchemeAsync(paperId);
        }

        [HttpGet("GetItemBanksFlattenedTreeNodes")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetItemBanksFlattenedTreeNodesAsync(long paperId)
        {
            return await _paperService.GetItemBanksFlattenedTreeNodesAsync(paperId);
        }

        [HttpGet("GetAllItemBanksFromItemBankPoints")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetAllItemBanksFromItemBankPointsAsync(long paperId)
        {
            return await _paperService.GetAllItemBanksFromItemBankPointsAsync(paperId);
        }

        [HttpGet("GetPaperDifficultyLevels")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetPaperDifficultyLevelsAsync(long paperId)
        {
            return await _paperService.GetPaperDifficultyLevelsAsync(paperId);
        }

        [HttpGet("GetCurrentPaperCreationStatus")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> GetCurrentPaperCreationStatusAsync(long paperId)
        {
            return await _paperService.GetCurrentPaperCreationStatusAsync(paperId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetAllPaperGroupsCreatedByUser")]
        public async Task<ApiResponse> GetAllPaperGroupsCreatedByUser()
        {
            return await _paperService.GetAllPaperGroupsCreatedByUser();
        }

        [OESFilter(Authorize = true)]
        [HttpGet("GetPaperGroups")]
        public async Task<IApiResponse> GetPaperGroupsAsync(long paperId)
        {
            return await _paperService.GetPaperGroupsAsync(paperId);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("IsPaperAssignedToSchedule")]
        public async Task<ApiResponse> IsPaperAssignedToScheduleAsync(long paperId)
        {
            return await _paperService.IsPaperAssignedToScheduleAsync(paperId);
        }


        // ADD ENDPOINTS

        [HttpPost("AddPaperMetaData")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddPaperMetaDataAsync(AddOrUpdatePaperMetadataRequestDto paperMetadataCreationRequestDto)
        {
            return await _paperService.AddPaperMetaDataAsync(paperMetadataCreationRequestDto);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("AddManualFormUsingExcelSheet")]
        public async Task<ApiResponse> AddManualFormUsingExcelSheetAsync(long paperId, AddOrUpdatePaperMetadataRequestDto addOrUpdatePaperMetadataRequestDto)
        {
            return await _paperService.AddManualFormUsingExcelSheetAsync(paperId, addOrUpdatePaperMetadataRequestDto);
        }

        [HttpPost("AddManuallySelectedQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddManuallySelectedQuestionsAsync(long paperId, AddOrUpdateManualQuestionsRequestDto addManualQuestionsRequestDto)
        {
            return await _paperService.AddManuallySelectedQuestionsAsync(paperId, addManualQuestionsRequestDto);
        }

        [HttpPost("AddOrUpdateManualQuestionSectioning")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddOrUpdateManualQuestionSectioningAsync(long paperId, ManualSectioningRequestDto request)
        {
            return await _paperService.AddOrUpdateManualQuestionSectioningAsync(paperId, request.Questions, request.FormSections, request.FormQuestionMap, request.FormsMetadata);
        }

        [HttpPost("AddOrUpdateSectionsDistributionsForAuto")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> AddOrUpdateSectionsDistributionsForAutoAsync([FromBody] AddOrUpdateAutoSectionsDistributionsRequestDto addOrUpdateAutoSectionsDistributionsRequestDto)
        {
            return await _paperService.AddOrUpdateSectionsDistributionsForAutoAsync(addOrUpdateAutoSectionsDistributionsRequestDto);
        }

        [HttpPost("SavePaperMetadataTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> SavePaperMetadataTemplateAsync(PaperMetadataSavingRequestDto temp)
        {
            return await _paperService.SavePaperMetadataTemplateAsync(temp);
        }

        [OESFilter(Authorize = true)]
        [HttpGet("CopyPaperAsync")]
        public async Task<IApiResponse> CopyPaperAsync(long paperId)
        {
            return await _paperService.CopyPaperAsync(paperId);
        }


        // UPDATE ENDPOINTS

        [HttpPut("UpdatePaperMetadata")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdatePaperMetadataAsync(long paperId, AddOrUpdatePaperMetadataRequestDto editPaperMetadataRequestDto)
        {
            return await _paperService.UpdatePaperMetadataAsync(paperId, editPaperMetadataRequestDto);
        }

        [HttpPut("UpdateManuallySelectedQuestions")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdateManuallySelectedQuestionsAsync(long paperId, long? formId, AddOrUpdateManualQuestionsRequestDto requestDto)
        {
            return await _paperService.UpdateManuallySelectedQuestionsAsync(paperId, formId, requestDto);
        }

        [HttpPut("UpdatePaperCreationStatus")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> UpdatePaperCreationStatusAsync(UpdatePaperCreationStatusRequestDto updatePaperCreationStatusRequestDto)
        {
            return await _paperService.UpdatePaperCreationStatusAsync(updatePaperCreationStatusRequestDto);
        }

        // DELETE ENDPOINTS

        [HttpDelete("DeletePaper")]
        [OESFilter(Authorize = true)]
        public async Task<IApiResponse> DeletePaperAsync(long id)
        {
            return await _paperService.DeletePaperAsync(id);
        }

        [HttpDelete("DeletePaperMetadataTemplate")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> DeletePaperMetadataTemplateAsync(long templateId)
        {
            return await _paperService.DeletePaperMetadataTemplateAsync(templateId);
        }

        // LOCK ENDPOINTS

        [OESFilter(Authorize = true)]
        [HttpPost("AcquirePaperLock")]
        public async Task<IApiResponse> AcquirePaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            return await _paperService.AcquirePaperLockAsync(paperLockRequestDto);
        }

        [OESFilter(Authorize = true)]
        [HttpPut("RenewPaperLock")]
        public async Task<IApiResponse> RenewPaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            return await _paperService.RenewPaperLockAsync(paperLockRequestDto);
        }

        [OESFilter(Authorize = true)]
        [HttpPost("ReleasePaperLock")]
        public async Task<IApiResponse> ReleasePaperLockAsync(PaperLockRequestDto paperLockRequestDto)
        {
            return await _paperService.ReleasePaperLockAsync(paperLockRequestDto);
        }
    }
}