using OES.Helper.Enums;

namespace OES.Helper.General
{
    /// <summary>
    /// Maps each ResourceType → its Pages → each page's allowed roles.
    /// Used by CreateTemplateDialog to render the 3-level tree: Resource → Page → Roles (checkboxes)
    /// </summary>
    public static class PageRoleMap
    {
        public record PageRoles(string PageName, string[] Roles);

        public static readonly Dictionary<ResourceType, List<PageRoles>> Map = new()
        {
            // ========================= QUESTIONS =========================
            [ResourceType.Questions] =
            [
                new("Questions", [
                    OesTemplateRoleConstants.Questionviewer,
                    OesTemplateRoleConstants.QuestionCreator,
                    OesTemplateRoleConstants.QuestionEditor,
                    OesTemplateRoleConstants.QuestionDeleter,
                    OesTemplateRoleConstants.QuestionReplacer,
                    OesTemplateRoleConstants.QuestionCopier,
                    OesTemplateRoleConstants.QuestionExamViewer,
                ]),
                new("Upload Question File", [
                    OesTemplateRoleConstants.QuestionFileUpload,
                ]),
                new("Export Questions", [
                    OesTemplateRoleConstants.QuestionExport,
                ]),
                new("Manage Question Delta Values", [
                    OesTemplateRoleConstants.QuestionDeltaUpload,
                ]),
                new("Questions Quality Check", [
                    OesTemplateRoleConstants.QuestionQualityChecker,
                    //OesTemplateRoleConstants.QuestionQualityCheckApproverAndRefuser,
                ]),
                new("Bypassing Questions Quality Check", [
                    OesTemplateRoleConstants.QuestionQualityCheckBypass,
                ]),
            ],

            // ========================= PAPERS (EXAM) =========================
            [ResourceType.Papers] =
            [
                new("User Exams", [
                    OesTemplateRoleConstants.Paperviewer,
                    OesTemplateRoleConstants.PaperCreator,
                    OesTemplateRoleConstants.PaperEditor,
                    OesTemplateRoleConstants.PaperDeleter,
                    OesTemplateRoleConstants.PaperUserExamViewer,
                    OesTemplateRoleConstants.PaperEquationViewer,
                    OesTemplateRoleConstants.PaperExamReset,
                    OesTemplateRoleConstants.PaperContinuePendingForm,
                    OesTemplateRoleConstants.PaperViewFormDetails,
                    OesTemplateRoleConstants.PaperSuspensionManger,
                ]),
                new("Equation Templates", [
                    OesTemplateRoleConstants.EquationCreator,
                    OesTemplateRoleConstants.EquationEditor,
                    OesTemplateRoleConstants.EquationDeleter,
                    OesTemplateRoleConstants.EquationViewer,
                    OesTemplateRoleConstants.EquationTempelateViewer,
                ]),
                new("Blocks", [
                    OesTemplateRoleConstants.BlockCreator,
                    OesTemplateRoleConstants.BlockEditor,
                    OesTemplateRoleConstants.BlockDeleter,
                    OesTemplateRoleConstants.BlockViewer,
                ]),
                new("Transition Profiles", [
                    OesTemplateRoleConstants.TransitionProfileCreate,
                    OesTemplateRoleConstants.TransitionProfileViewer,
                    OesTemplateRoleConstants.TransitionProfileEdit,
                    OesTemplateRoleConstants.TransitionProfileDelete,
                ]),
                 new("Transition Levels", [
                    OesTemplateRoleConstants.TransitionLevelCreate,
                    OesTemplateRoleConstants.TransitionLevelViewer,
                    OesTemplateRoleConstants.TransitionLevelEdit,
                    OesTemplateRoleConstants.TransitionLevelDelete,
                ]),
                new("Examination Templates", [
                    OesTemplateRoleConstants.PaperExamSettingstempelate,
                ]),
                new("Exam Screen", [
                    OesTemplateRoleConstants.ExamScreenViewer,
                ]),
            ],

            // ========================= SCHEDULE =========================
            [ResourceType.Schedule] =
            [
                new("Schedules", [
                    OesTemplateRoleConstants.ScheduleViewer,
                    OesTemplateRoleConstants.ScheduleCreator,
                    OesTemplateRoleConstants.ScheduleEditor,
                    OesTemplateRoleConstants.ScheduleDeleter,
                    OesTemplateRoleConstants.ScheduleApprover,
                    OesTemplateRoleConstants.ScheduleTracker,
                    OesTemplateRoleConstants.ScheduleExamView,
                ]),
                new("Venues", [
                    OesTemplateRoleConstants.VenueViewer,
                    OesTemplateRoleConstants.VenueCreator,
                    OesTemplateRoleConstants.VenueEditor,
                    OesTemplateRoleConstants.VenueDeleter,
                    OesTemplateRoleConstants.VenueImporter,
                ]),
                new("Schedule Security Templates", [
                    OesTemplateRoleConstants.ScheduleSecurityTempelate,
                ]),
                new("Exam Settings Templates", [
                    OesTemplateRoleConstants.ScheduleExamSettingsTempelate,
                ]),
                new("Sync Status", [
                    OesTemplateRoleConstants.ScheduleSync,
                    OesTemplateRoleConstants.ScheduleCbtSyncer,
                    OesTemplateRoleConstants.ScheduleCbtViewer,
                    OesTemplateRoleConstants.CandidateAnswersToEvaluationSyncer,
                ]),
                new("Security Configuration View", [
                    OesTemplateRoleConstants.SecurityConfigurationViewer,
                ]),
                new("Paper Setting Template View", [
                    OesTemplateRoleConstants.PaperSettingTemplateViewer,
                ]),
            ],

            // ========================= CANDIDATES =========================
            [ResourceType.Candidate] =
            [
                new("Candidate List", [
                    OesTemplateRoleConstants.CandidateList,
                    OesTemplateRoleConstants.CandidateViewer,
                    OesTemplateRoleConstants.CandidateEditor,
                    OesTemplateRoleConstants.CandidateDeleter,
                    OesTemplateRoleConstants.CandidateImport,
                ]),
                new("Candidates Schedules", [
                    OesTemplateRoleConstants.CandidateSchedules,
                ]),
                new("Candidates Result List", [
                    OesTemplateRoleConstants.CandidateResultView,
                ]),
                new("Candidate Extra Time", [
                    OesTemplateRoleConstants.CandidateExtraTime,
                ]),
                new("Candidate Verification Code Generator", [
                    OesTemplateRoleConstants.CandidateVerificationCodeGenerator,
                ]),
                new("Candidate Schedule Papers", [
                     OesTemplateRoleConstants.CandidateSchedulePapersViewer,
                ]),
            ],

            // ========================= Reports =========================
            [ResourceType.Report] =
            [
                new("Report", [
                    OesTemplateRoleConstants.ReportGenerator,
                    OesTemplateRoleConstants.ReportGeneratorViewer,
                ]),
                new("Question Indicators", [
                    OesTemplateRoleConstants.ReportQuestionIndicator,
                ]),
            ],

            // ========================= RESULTS =========================
            [ResourceType.Result] =
            [
                new("Result Generation", [
                    OesTemplateRoleConstants.ResultGenerate,
                ]),
                new("Results Status", [
                    OesTemplateRoleConstants.ResultStatus,
                ]),
                new("Review Unfinished", [
                    OesTemplateRoleConstants.ResultReviewUnfinished,
                ]),
                new("Candidate Results", [
                    OesTemplateRoleConstants.ResultCandidateResults,
                    OesTemplateRoleConstants.ResultReviewer,
                ]),
                new("Tracking Logs", [
                    OesTemplateRoleConstants.ResultTrackingLogs,
                ]),
            ],

            // ========================= FILE MANAGER =========================
            [ResourceType.FileManger] =
            [
                new("File Manager", [
                    OesTemplateRoleConstants.FileMangerAdmin,
                ]),
            ],

            // ========================= ITEM BANK =========================
            [ResourceType.ItemBank] =
            [
                new("Item Banks", [
                    OesTemplateRoleConstants.ItemBankViewer,
                    OesTemplateRoleConstants.ItemBankCreator,
                    OesTemplateRoleConstants.ItemBankEditor,
                    OesTemplateRoleConstants.ItemBankDeleter,
                    OesTemplateRoleConstants.ItemBankQuestionEditor,
                    OesTemplateRoleConstants.ItemBankQuestionReplacer,
                    OesTemplateRoleConstants.ItemBankQuestionDeleter,
                    OesTemplateRoleConstants.ItemBankQuestionViewer,
                    OesTemplateRoleConstants.ItemBankQuestionCreator,
                    OesTemplateRoleConstants.ItemBankQuestionQualityChecker,
                ]),
            ],

            // ========================= ILO =========================
            [ResourceType.Ilo] =
            [
                new("ILOs", [
                    OesTemplateRoleConstants.IloViewer,
                    OesTemplateRoleConstants.IloCreator,
                    OesTemplateRoleConstants.IloEditor,
                    OesTemplateRoleConstants.IloDeleter,
                ]),
            ],

            // ========================= CONFIGURATIONS =========================
            [ResourceType.Configurations] =
            [
                new("Question Categories", [
                    OesTemplateRoleConstants.QuestionCategoryViewer,
                    OesTemplateRoleConstants.QuestionCategoryCreator,
                    OesTemplateRoleConstants.QuestionCategoryEditor,
                    OesTemplateRoleConstants.QuestionCategoryDeleter,
                ]),
                new("Delta Types", [
                    OesTemplateRoleConstants.DeltaTypeViewer,
                    OesTemplateRoleConstants.DeltaTypeDeleter,
                    OesTemplateRoleConstants.DeltaTypeCreator,
                    OesTemplateRoleConstants.DeltaTypeEditor,
                ]),
                new("Difficulty Profiles", [
                    OesTemplateRoleConstants.DifficultyProfileViewer,
                    OesTemplateRoleConstants.DifficultyProfileCreator,
                    OesTemplateRoleConstants.DifficultyProfileEditor,
                    OesTemplateRoleConstants.DifficultyProfileDeleter,
                ]),
                new("Difficulty Levels", [
                    OesTemplateRoleConstants.DifficultyLevelViewer,
                    OesTemplateRoleConstants.DifficultyLevelCreator,
                    OesTemplateRoleConstants.DifficultyLevelEditor,
                    OesTemplateRoleConstants.DifficultyLevelDeleter,
                ]),
                new("Defined Languages", [
                    OesTemplateRoleConstants.DefinedLanguageCreator,
                    OesTemplateRoleConstants.DefinedLanguageEditor,
                    OesTemplateRoleConstants.DefinedLanguageDeleter,
                ]),
                new("Defined Materials", [
                    OesTemplateRoleConstants.DefinedMaterialViewer,
                    OesTemplateRoleConstants.DefinedMaterialCreator,
                    OesTemplateRoleConstants.DefinedMaterialEditor,
                    OesTemplateRoleConstants.DefinedMaterialDeleter,
                ]),
                new("Media Configuration", [
                    OesTemplateRoleConstants.MediaConfigurationViewer,
                    OesTemplateRoleConstants.MediaConfigurationEditor,
                    OesTemplateRoleConstants.MediaConfigurationDeleter,
                ]),
                new("Questions Quality Check Committees", [
                    OesTemplateRoleConstants.QualityCheckCommitteeViewer,
                    OesTemplateRoleConstants.QualityCheckCommitteeCreator,
                    OesTemplateRoleConstants.QualityCheckCommitteeEditor,
                ]),
                new("Cbt AutoSettings Configuration", [
                    OesTemplateRoleConstants.CbtAutoSettingsEditor,
                    OesTemplateRoleConstants.CbtAutoSettingsViewer,
                ]),
                new("Special Cases For Adding Extra Time", [
                    OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeDeleter,
                    OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeEditor,
                    OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeCreator,
                ]),
            ],
        };

        /// <summary>
        /// Returns all roles that belong to the given resources (flattened).
        /// Used to clean up SelectedRoleNames when resources are deselected.
        /// </summary>
        public static HashSet<string> GetAllRolesFor(IEnumerable<ResourceType> resources)
            => resources
                .Where(Map.ContainsKey)
                .SelectMany(r => Map[r].SelectMany(p => p.Roles))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
