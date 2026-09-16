namespace OES.Helper.Dtos.AuditLogs
{
    public static class AuditLogPageResolver
    {
        private static readonly List<PageMapping> PageMappings = new()
        {
            new PageMapping
            {
                PageName = "ItemBankList",
                Paths = ["/ItemBankList", "/AddItemBank", "/Update"]
            },

            // Questions
            new PageMapping
            {
                PageName = "Questions",
                Paths = ["/Questions", "/QuestionCreation"]
            },
            new PageMapping
            {
                PageName = "UploadFile",
                Paths = ["/UploadFile"]
            },
            new PageMapping
            {
                PageName = "ExportFile",
                Paths = ["/ExportFile"]
            },
            new PageMapping
            {
                PageName = "QuestionDeltaUpload",
                Paths = ["/QuestionDeltaUpload"]
            },
            new PageMapping
            {
                PageName = "QuestionsQualityCheck",
                Paths = ["/QuestionsQualityCheck"]
            },
            new PageMapping
            {
                PageName = "BypassQuestionsQualityCheck",
                Paths = ["/BypassQuestionsQualityCheck"]
            },

            // Exam
            new PageMapping
            {
                PageName = "UserPapers",
                Paths = ["/UserPapers", "/CreateOrUpdatePaper"]
            },
            new PageMapping
            {
                PageName = "EquationTemplateList",
                Paths = ["/AddEquationTemplate", "/UpdateEquationTemplate", "/EquationTemplateList"]
            },
            new PageMapping
            {
                PageName = "BlocksList",
                Paths = ["/BlocksList", "/CreateNewBlock", "/EditBlock"]
            },
            new PageMapping
            {
                PageName = "TransitionProfiles",
                Paths = ["/TransitionProfiles", "/AddTransitionProfile", "/EditTransitionProfile"]
            },
            new PageMapping
            {
                PageName = "TransitionLevels",
                Paths = ["/TransitionLevels", "/AddTransitionLevel", "/UpdateTransitionLevel"]
            },
            new PageMapping
            {
                PageName = "ExaminationTemplates",
                Paths = ["/ExaminationTemplates"]
            },

            // Schedules
            new PageMapping
            {
                PageName = "Schedules",
                Paths = ["/Schedules", "/CreateOrUpdateSchedule"]
            },
            new PageMapping
            {
                PageName = "Venues",
                Paths = ["/Venues", "/AddVenue", "/EditVenue"]
            },
            new PageMapping
            {
                PageName = "ScheduleSecurityTemplates",
                Paths = ["/ScheduleSecurityTemplates", "/CreateScheduleSecurityConfigurationTemplate", "/EditScheduleSecurityConfigurationTemplate"]
            },
            new PageMapping
            {
                PageName = "PaperSettingTemplates",
                Paths = ["/CreatePaperSettingsTemplate", "/EditPaperSettingsTemplate", "/PaperSettingTemplates"]
            },
            new PageMapping
            {
                PageName = "SyncStatus",
                Paths = ["/SyncStatus"]
            },

            // Candidates
            new PageMapping
            {
                PageName = "Candidates",
                Paths = ["/Candidates", "/UpdateCandidate"]
            },
            new PageMapping
            {
                PageName = "CandidateSchedules",
                Paths = ["/CandidateSchedules"]
            },
            new PageMapping
            {
                PageName = "CandidateSchedulePapersList",
                Paths = ["/CandidateSchedulePapersList"]
            },
            new PageMapping
            {
                PageName = "CandidatesResultList",
                Paths = ["/CandidatesResultList"]
            },
            new PageMapping
            {
                PageName = "AddCandidateExtraTimeList",
                Paths = ["/AddCandidateExtraTimeList"]
            },
            new PageMapping
            {
                PageName = "VerificationCodeGenerator",
                Paths = ["/VerificationCodeGenerator"]
            },

            // Result
            new PageMapping
            {
                PageName = "ResultGeneration",
                Paths = ["/ResultGeneration"]
            },
            new PageMapping
            {
                PageName = "ResultsStatus",
                Paths = ["/ResultsStatus"]
            },
            new PageMapping
            {
                PageName = "ReviewUnfinished",
                Paths = ["/ReviewUnfinished"]
            },
            new PageMapping
            {
                PageName = "CandidateResults",
                Paths = ["/CandidateResults"]
            },

            // Reports
            new PageMapping
            {
                PageName = "reports/item-analysis",
                Paths = ["/reports/item-analysis"]
            },
            new PageMapping
            {
                PageName = "QuestionIndicators",
                Paths = ["/QuestionIndicators"]
            },

            // File
            new PageMapping
            {
                PageName = "FileManager",
                Paths = ["/FileManager"]
            },

            // Configurations
            new PageMapping
            {
                PageName = "QuestionCategory",
                Paths = ["/AddCategory", "/QuestionCategory", "/UpdateCategory"]
            },
            new PageMapping
            {
                PageName = "DeltaTypeList",
                Paths = ["/DeltaTypeList"]
            },
            new PageMapping
            {
                PageName = "DifficultyProfile",
                Paths = ["/DifficultyProfile", "/AddDifficultyProfile", "/UpdateProfile"]
            },
            new PageMapping
            {
                PageName = "DifficultyLevel",
                Paths = ["/DifficultyLevel", "/AddDifficultyLevel", "/UpdateDifficultyLevel"]
            },
            new PageMapping
            {
                PageName = "Language",
                Paths = ["/Language", "/AddLanguage", "/EditLanguage"]
            },
            new PageMapping
            {
                PageName = "SubjectList",
                Paths = ["/SubjectList", "/AddSubject", "/UpdateSubject"]
            },
            new PageMapping
            {
                PageName = "MediaConfiguration",
                Paths = ["/MediaConfiguration"]
            },
            new PageMapping
            {
                PageName = "QualityCheckCommittees",
                Paths = ["/QualityCheckCommittees", "/AddQualityCheckCommittee", "/UpdateQualityCheckCommittee"]
            },
            new PageMapping
            {
                PageName = "CBTSyncSetting",
                Paths = ["/CBTSyncSetting", "/UpdateCBTSyncSetting"]
            },
            new PageMapping
            {
                PageName = "Disabilities",
                Paths = ["/Disabilities", "/AddDisability", "/UpdateDisability"]
            },

            // Authentication
            new PageMapping
            {
                PageName = "AppUserList",
                Paths = ["/AppUserList"]
            },
            new PageMapping
            {
                PageName = "AllGroupsList",
                Paths = ["/AllGroupsList"]
            },
            new PageMapping
            {
                PageName = "AssignRoleToPage",
                Paths = ["/AssignRoleToPage"]
            },
            new PageMapping
            {
                PageName = "AuditLogs",
                Paths = ["/AuditLogs"]
            }
        };

        public static string Resolve(string? pathName)
        {
            if (string.IsNullOrWhiteSpace(pathName))
                return string.Empty;

            var normalizedPath = pathName.Trim().TrimEnd('/');

            var mapping = PageMappings.FirstOrDefault(m => m.Paths.Any(p => normalizedPath.Equals(p, StringComparison.OrdinalIgnoreCase)));

            return mapping?.PageName ?? normalizedPath.TrimStart('/');
        }

        public static List<string> GetAllPageNames()
        {
            return PageMappings.Select(m => m.PageName).Distinct().OrderBy(x => x).ToList();
        }
    }
}
