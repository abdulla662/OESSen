using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using SharedHelper.General;
using SharedHelper.RolesNames;

namespace OES.Blazor.Shared.Layout
{
    public partial class NavMenu
    {
        [Inject] private IBlazAuthService BlazAuthService { get; set; } = default!;
        [Inject] private IBlazAIFeatureAccessService AIFeatureAccessService { get; set; } = default!;
        [Inject] public GlobalUserContext GlobalUserContext { get; set; } = default!;
        [Inject] private CultureService CultureService { get; set; } = default!;

        private string SsoUrl { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            GlobalUserContext.HasAIFeaturesAccess = await AIFeatureAccessService.HasAIFeaturesAccessAsync(GlobalUserContext.CurrentOrganizationId);

            var culture = await CultureService.GetCultureAsync();
            SsoUrl = $"{CentralizedUrlHelper.SsoBlazorBaseUrl}handler?token={await BlazAuthService.GetDecryptedTokenFromLocalStorageAsync()}&fromOrigin={CentralizedUrlHelper.OesBlazorBaseUrl}&culture={culture}";
        }

        public static readonly string[] QuestionRoles =
        [
            OesTemplateRoleConstants.QuestionCreator,
            OesTemplateRoleConstants.QuestionEditor,
            OesTemplateRoleConstants.QuestionDeleter,
            OesTemplateRoleConstants.QuestionCopier,
            OesTemplateRoleConstants.Questionviewer,
            OesTemplateRoleConstants.QuestionReplacer,
            OesTemplateRoleConstants.ItemBankQuestionCreator,
            OesTemplateRoleConstants.ItemBankQuestionEditor,
            OesTemplateRoleConstants.ItemBankQuestionDeleter,
            OesTemplateRoleConstants.ItemBankQuestionViewer,
            OesTemplateRoleConstants.QuestionExamViewer,
            OesTemplateRoleConstants.QuestionFileUpload,
            OesTemplateRoleConstants.QuestionExport,
            OesTemplateRoleConstants.QuestionQualityChecker,
            OesTemplateRoleConstants.QuestionQualityCheckBypass
        ];

        public static readonly string[] PaperRoles =
        [
            OesTemplateRoleConstants.PaperCreator,
            OesTemplateRoleConstants.PaperEditor,
            OesTemplateRoleConstants.PaperDeleter,
            OesTemplateRoleConstants.Paperviewer,
            OesTemplateRoleConstants.PaperUserExamViewer
        ];

        public static readonly string[] ScheduleRoles =
        [
            OesTemplateRoleConstants.ScheduleCreator,
            OesTemplateRoleConstants.ScheduleEditor,
            OesTemplateRoleConstants.ScheduleDeleter,
            OesTemplateRoleConstants.ScheduleApprover,
            OesTemplateRoleConstants.ScheduleViewer,
            OesTemplateRoleConstants.ScheduleTracker,
            OesTemplateRoleConstants.ScheduleSync,
            OesTemplateRoleConstants.ScheduleSecurityTempelate,
            OesTemplateRoleConstants.ScheduleExamSettingsTempelate,
            OesTemplateRoleConstants.ScheduleCbtSyncer,
            OesTemplateRoleConstants.ScheduleCbtViewer
        ];

        public static readonly string[] ItemBankRoles =
        [
            OesTemplateRoleConstants.ItemBankCreator,
            OesTemplateRoleConstants.ItemBankEditor,
            OesTemplateRoleConstants.ItemBankDeleter,
            OesTemplateRoleConstants.ItemBankViewer
        ];

        public static readonly string[] IloRoles =
        [
            OesTemplateRoleConstants.IloCreator,
            OesTemplateRoleConstants.IloEditor,
            OesTemplateRoleConstants.IloDeleter,
            OesTemplateRoleConstants.IloViewer,
        ];

        public static readonly string[] ResultRoles =
        [
            OesTemplateRoleConstants.ResultCandidateResults,
            OesTemplateRoleConstants.ResultGenerate,
            OesTemplateRoleConstants.ResultReviewUnfinished,
            OesTemplateRoleConstants.ResultStatus,
        ];

        public static readonly string[] BlockRoles =
        [
            OesTemplateRoleConstants.BlockCreator,
            OesTemplateRoleConstants.BlockEditor,
            OesTemplateRoleConstants.BlockDeleter,
            OesTemplateRoleConstants.BlockViewer
        ];

        public static readonly string[] EquationRoles =
        [
            OesTemplateRoleConstants.EquationCreator,
            OesTemplateRoleConstants.EquationEditor,
            OesTemplateRoleConstants.EquationDeleter,
            OesTemplateRoleConstants.EquationViewer,
            OesTemplateRoleConstants.EquationTempelateViewer
        ];

        public static readonly string[] CandidateRoles =
        [
            OesTemplateRoleConstants.CandidateList,
            OesTemplateRoleConstants.CandidateViewer,
            OesTemplateRoleConstants.CandidateEditor,
            OesTemplateRoleConstants.CandidateDeleter,
            OesTemplateRoleConstants.CandidateImport,
            OesTemplateRoleConstants.CandidateSchedules,
            OesTemplateRoleConstants.CandidateResultView,
            OesTemplateRoleConstants.CandidateExtraTime,
            OesTemplateRoleConstants.CandidateVerificationCodeGenerator,
        ];

        public static readonly string[] TransitionProfileRoles =
        [
            OesTemplateRoleConstants.TransitionProfile,
            OesTemplateRoleConstants.TransitionProfileCreate,
            OesTemplateRoleConstants.TransitionProfileViewer,
            OesTemplateRoleConstants.TransitionProfileEdit,
            OesTemplateRoleConstants.TransitionProfileDelete
        ];

        public static readonly string[] TransitionLevelRoles =
        [
            OesTemplateRoleConstants.TransitionLevel,
            OesTemplateRoleConstants.TransitionLevelCreate,
            OesTemplateRoleConstants.TransitionLevelViewer,
            OesTemplateRoleConstants.TransitionLevelEdit,
            OesTemplateRoleConstants.TransitionLevelDelete
        ];

        public static readonly string[] VenueRoles =
        [
            OesTemplateRoleConstants.VenueCreator,
            OesTemplateRoleConstants.VenueImporter,
            OesTemplateRoleConstants.VenueViewer,
            OesTemplateRoleConstants.VenueEditor,
            OesTemplateRoleConstants.VenueDeleter
        ];

        public static readonly string[] FileManagerRoles =
        [
            OesTemplateRoleConstants.FileMangerAdmin
        ];

        public static readonly string[] QuestionCategoryRoles =
        [
            OesTemplateRoleConstants.QuestionCategoryCreator,
            OesTemplateRoleConstants.QuestionCategoryEditor,
            OesTemplateRoleConstants.QuestionCategoryDeleter,
            OesTemplateRoleConstants.QuestionCategoryViewer
        ];

        public static readonly string[] DeltaTypeRoles =
        [
            OesTemplateRoleConstants.DeltaTypeDeleter,
            OesTemplateRoleConstants.DeltaTypeViewer
        ];

        public static readonly string[] DifficultyProfileRoles =
        [
            OesTemplateRoleConstants.DifficultyProfileCreator,
            OesTemplateRoleConstants.DifficultyProfileEditor,
            OesTemplateRoleConstants.DifficultyProfileDeleter,
            OesTemplateRoleConstants.DifficultyProfileViewer
        ];

        public static readonly string[] DifficultyLevelRoles =
        [
            OesTemplateRoleConstants.DifficultyLevelCreator,
            OesTemplateRoleConstants.DifficultyLevelEditor,
            OesTemplateRoleConstants.DifficultyLevelDeleter,
            OesTemplateRoleConstants.DifficultyLevelViewer
        ];

        public static readonly string[] LanguageRoles =
        [
            OesTemplateRoleConstants.DefinedLanguageCreator,
            OesTemplateRoleConstants.DefinedLanguageEditor,
            OesTemplateRoleConstants.DefinedLanguageDeleter
        ];

        public static readonly string[] MaterialRoles =
        [
            OesTemplateRoleConstants.DefinedMaterialCreator,
            OesTemplateRoleConstants.DefinedMaterialEditor,
            OesTemplateRoleConstants.DefinedMaterialDeleter,
            OesTemplateRoleConstants.DefinedMaterialViewer
        ];

        public static readonly string[] MediaConfigurationRoles =
        [
            OesTemplateRoleConstants.MediaConfigurationEditor,
            OesTemplateRoleConstants.MediaConfigurationDeleter,
            OesTemplateRoleConstants.MediaConfigurationViewer
        ];

        public static readonly string[] QualityCheckCommitteeRoles =
        [
            OesTemplateRoleConstants.QualityCheckCommitteeCreator,
            OesTemplateRoleConstants.QualityCheckCommitteeEditor,
            OesTemplateRoleConstants.QualityCheckCommitteeViewer
        ];

        public static readonly string[] AutoSyncConfigurationRoles =
        [
            OesTemplateRoleConstants.CbtAutoSettingsEditor,
            OesTemplateRoleConstants.CbtAutoSettingsViewer,
        ];

        public static readonly string[] SpecialCasesRoles =
        [
            OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeEditor,
            OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeCreator,
            OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeDeleter
        ];

        public static readonly string[] ReportRole =
        [
            OesTemplateRoleConstants.ReportGeneratorViewer,
            OesTemplateRoleConstants.ReportGenerator,
            OesTemplateRoleConstants.ReportQuestionIndicator
        ];

        public static readonly string[] BaseAdminRoles =
        [
            AdminRoles.Admin,
            AdminRoles.SuperAdmin,
            AdminRoles.Entity_Admin,
        ];

        public static readonly string[] AuditLogsRoles =
        [
            OesTemplateRoleConstants.AuditLogsViewer,
            OesTemplateRoleConstants.AuditLogsExporter,
        ];

        private bool HasAccess(IEnumerable<string> rolesToCheck)
        {
            return ListHelpers.HaveCommonValues(GlobalUserContext.SsoUserRoles.ConvertAll(r => r.Name), [.. BaseAdminRoles]) ||
                   ListHelpers.HaveCommonValues(GlobalUserContext.OesUserRoles.ConvertAll(r => r.Name), [.. rolesToCheck]);
        }
    }
}