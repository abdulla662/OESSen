using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.CBTCandidates;
using OES.Core.Entities.CBTCandidates.Views;
using OES.Core.Entities.CTRExam;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Paper.Views;
using OES.Core.Entities.QuestionQualityCheck;
using OES.Core.Entities.Schedule;
using OES.Core.Entities.Schedule.Views;
using OES.Core.Entities.Views.ItemBanks;
using OES.Core.Entities.Views.Results;
using OES.Helper.General;
using OES.Infrastructure.Extensions;
using OES.Infrastructure.ValueConverters;
using SharedHelper.General;
using System.Linq.Expressions;
using Attribute = OES.Core.Entities.Paper.Attribute;
using PendingAnswersToEvaluate = OES.Core.Entities.Views.Answers.PendingAnswersToEvaluate;

namespace OES.Infrastructure.Contexts
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public (string User, long OrganizationId, string OrganizationSignature, DateTime CreationDate)? ExplicitAudit { get; set; }
        public FilterParamsValues FilterParamsValues { set; get; } = new FilterParamsValues();

        public DbSet<ItemBank> ItemBanks { get; set; }
        public DbSet<ItemBankLevel> ItemBankLevels { get; set; }
        public DbSet<ItemBankGroups> ItemBankGroups { get; set; }

        public DbSet<ILO> ILOs { get; set; }
        public DbSet<ILOGroup> ILOGroups { get; set; }

        public DbSet<QuestionMetadata> QuestionsMetaData { get; set; }
        public DbSet<QuestionDetails> QuestionsDetails { get; set; }
        public DbSet<QuestionType> QuestionTypes { get; set; }
        public DbSet<QuestionCategory> QuestionCategories { get; set; }
        public DbSet<QuestionCategoryGroups> QuestionCategoryGroups { get; set; }
        public DbSet<QuestionLayout> QuestionLayouts { get; set; }
        public DbSet<QuestionTemplate> QuestionTemplates { get; set; }
        public DbSet<ItemBankTemplate> ItemBankTemplates { get; set; }
        public DbSet<QuestionInstructionTemplate> QuestionInstructionTemplates { get; set; }
        public DbSet<QuestionsChoices> QuestionsChoices { get; set; }
        public DbSet<QCComment> QCComments { get; set; }
        public DbSet<QuestionDocLibFile> QuestionDocLibFiles { get; set; }
        public DbSet<QuestionUploadTemplate> UploadQuestionTemplates { get; set; }
        public DbSet<QuestionGroups> QuestionGroups { get; set; }

        public DbSet<QualityCheckCommittee> QualityCheckCommittees { get; set; }
        public DbSet<QualityCheckCommitteeGroups> QualityCheckCommitteeGroups { get; set; }
        public DbSet<QualityCheckCommitteeMember> QualityCheckCommitteeMembers { get; set; }
        public DbSet<QuestionCommitteeAssignment> QuestionCommitteeAssignments { get; set; }
        public DbSet<QuestionReview> QuestionReviews { get; set; }
        public DbSet<QualityCheckCommitteeItemBank> QualityCheckCommitteeItemBanks { get; set; }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<SubjectGroups> SubjectGroups { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LanguageGroups> LanguageGroups { get; set; }
        public DbSet<FileDetails> FileDetails { get; set; }
        public DbSet<DeltaType> DeltaTypes { get; set; }
        public DbSet<DeltaTypeGroups> DeltaTypeGroups { get; set; }
        public DbSet<DifficultyLevel> DifficultyLevels { get; set; }
        public DbSet<DifficultyLevelGroups> DifficultyLevelGroups { get; set; }
        public DbSet<DifficultyProfile> DifficultyProfiles { get; set; }
        public DbSet<DifficultyProfileGroups> DifficultyProfileGroups { get; set; }

        public DbSet<ApiEndpoint> ApiEndpoints { get; set; }
        public DbSet<ApiEndpointRole> ApiEndpointRoles { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<PageRoles> PageRoles { get; set; }

        public DbSet<AppUserProfile> AppUserProfiles { get; set; }
        public DbSet<AppUserProfileSubject> AppUserProfileSubjects { get; set; }
        public DbSet<AppUserProfileGroup> AppUserProfileGroups { get; set; }
        public DbSet<NotificationAppUserProfile> NotificationAppUserProfile { get; set; }
        public DbSet<OESGroup> OESGroups { get; set; }
        public DbSet<OESRole> OESRoles { get; set; }
        public DbSet<OESGroupRole> OESGroupsRoles { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<PaperMetadata> PaperMetadata { get; set; }
        public DbSet<PaperSubject> PaperSubjects { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<BlockGroups> BlockGroups { get; set; }
        public DbSet<Stage> Stages { get; set; }
        public DbSet<StandardSection> Sections { get; set; }
        public DbSet<PaperItemBankPoint> ItemBankPoints { get; set; }
        public DbSet<ManualPaperItemBankQuestionSection> ManualPaperItemBankQuestionSections { get; set; }
        public DbSet<AutoPaperItemBankQuestionSection> AutoPaperItemBankQuestionSections { get; set; }
        public DbSet<BlockQuestion> BlockQuestions { get; set; }
        public DbSet<PaperFormBlock> PaperFormBlocks { get; set; }
        public DbSet<PaperGroups> PaperGroups { get; set; }

        public DbSet<MarkingScheme> ScoringSchemes { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateAttribute> TemplateAttributes { get; set; }
        public DbSet<TemplateType> TemplateTypes { get; set; }
        public DbSet<Attribute> Attributes { get; set; }
        public DbSet<TransitionProfile> TransitionProfiles { get; set; }
        public DbSet<TransitionProfileGroups> TransitionProfileGroups { get; set; }
        public DbSet<TransitionLevel> TransitionLevels { get; set; }
        public DbSet<EquationTemplate> EquationTemplates { get; set; }
        public DbSet<EquationGroups> EquationGroups { get; set; }
        public DbSet<PaperItemBankEquation> PaperItemBankEquations { get; set; }
        public DbSet<EquationCategory> EquationCategories { get; set; }

        public DbSet<ScheduleMetadata> ScheduleMetadata { get; set; }
        public DbSet<ScheduleTemplate> ScheduleTemplates { get; set; }
        public DbSet<ScheduleLanguage> ScheduleLanguages { get; set; }
        public DbSet<ScheduleVenue> ScheduleVenues { get; set; }
        public DbSet<ScheduleSecurityConfiguration> ScheduleSecurityConfigurations { get; set; }
        public DbSet<SchedulePaper> SchedulePapers { get; set; }
        public DbSet<SchedulePaperSettings> PaperSettings { get; set; }
        public DbSet<PaperMetadataTemplate> PaperMetadataTemplates { get; set; }
        public DbSet<SchedulePaperCandidate> SchedulePapersCandidates { get; set; }
        public DbSet<PaperSettingTemplate> PaperSettingTemplates { get; set; }
        public DbSet<ScheduleSecurityConfigurationTemplate> ScheduleSecurityConfigurationTemplates { get; set; }
        public DbSet<ScheduleGroups> ScheduleGroups { get; set; }
        public DbSet<OESResource> OESResources { get; set; }
        public DbSet<OESGroupResource> OESGroupResources { get; set; }
        public DbSet<OESGroupResourceRole> OESGroupResourceRoles { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<CandidateGroups> CandidateGroups { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<VenueGroups> VenueGroups { get; set; }
        public DbSet<OrganizationStructure> OrganizationsStructures { get; set; }
        public DbSet<OrganizationNodeLookupItem> OrganizationNodeLookupItems { get; set; }
        public DbSet<CandidateOrganizationNodeLookupItem> CandidatesOrganizationNodeLookupItems { get; set; }
        public DbSet<PaperForm> Forms { get; set; }
        public DbSet<GeneratedFormQuestion> FormQuestions { get; set; }
        public DbSet<RealTimeSyncJob> RealTimeSyncJobs { get; set; }
        public DbSet<CandidateQuestionsAnswers> CandidateQuestionsAnswers { get; set; }
        public DbSet<CBTCandidatesRecievedData> CBTCandidatesReceivedData { get; set; }
        public DbSet<CBTCandidatesScyncJobs> CBTCandidatesSyncJobs { get; set; }
        public DbSet<PaperStageCategoryDecisionPath> PaperStageCategoryDecisionPaths { get; set; }
        public DbSet<CandidateExamDetails> CandidateExamDetails { get; set; }
        public DbSet<CTRExamSyncJobs> CTRExamSyncJobs { get; set; }
        public DbSet<CTRResultsFileName> CTRResultsFileNames { get; set; }
        public DbSet<BlockCandidateAnswer> BlockCandidateAnswers { get; set; }
        public DbSet<SegmentQuestionProperties> SegmentQuestionProperties { get; set; }
        public DbSet<MediaSetting> MediaSettings { get; set; }
        public DbSet<CBTSyncSetting> CBTSyncSettings { get; set; }
        public DbSet<MediaSettingsGroups> MediaSettingsGroup { get; set; }
        public DbSet<QuestionDetailsVersions> QuestionDetailsVersions { get; set; }
        public DbSet<QuestionsChoicesVersions> QuestionsChoicesVersions { get; set; }
        public DbSet<CandidateTrackingLog> CandidateTrackingLogs { get; set; }
        public DbSet<PaperVenueSuspension> PaperVenueSuspensions { get; set; }
        public DbSet<FormVenueSuspension> FormVenueSuspensions { get; set; }
        public DbSet<Disability> Disabilities { get; set; }
        public DbSet<MatchingPairQuestionItems> MatchingPairQuestionItems { get; set; }
        public DbSet<SchedulePaperForms> SchedulePaperForms { get; set; }
        public DbSet<EvaluationSyncJob> EvaluationSyncJobs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SegmentQuestionPropertiesVersions> SegmentQuestionPropertiesVersions { get; set; }
        public DbSet<MatchingPairQuestionItemsVersions> MatchingPairQuestionItemsVersions { get; set; }

        // Views

        public DbSet<QuestionTypeCountView> QuestionTypeCountView { get; set; }
        public DbSet<AutoQuestionSummaryView> AutoQuestionSummaryView { get; set; }
        public DbSet<ManualQuestionSummaryView> ManualQuestionSummaryView { get; set; }
        public DbSet<FlattenedTreeNodeView> FlattenedTreeNodeView { get; set; }
        public DbSet<SchedulePaperView> SchedulePaperView { get; set; }
        public DbSet<PaperStageSectionBlockSummaryView> PaperStageSectionBlockSummaryView { get; set; }
        public DbSet<SchedulePaperAllocationView> SchedulePaperAllocationView { get; set; }
        public DbSet<CandidateQuestionsWithEquationView> CandidateQuestionsWithEquationView { get; set; }
        public DbSet<CBTSyncStatusView> CBTSyncStatusView { get; set; }
        public DbSet<CentersSyncStatusView> CentersSyncStatusView { get; set; }
        public DbSet<PaperFormAttendanceReportView> PaperFormAttendanceReportView { get; set; }
        public DbSet<ItemBanksFromQuestionsBlocksView> ItemBanksFromQuestionsBlocksView { get; set; }
        public DbSet<SyncStatusOESToCESReportView> SyncStatusOESToCESReportView { get; set; }
        public DbSet<ItemBanksSummaryReportView> ItemBanksSummaryReportView { get; set; }
        public DbSet<PendingAnswersToEvaluate> PendingAnswersToEvaluate { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Global query filters configurations:

            SetGlobalIsDeletedFilterToAllEntities(modelBuilder);


            // Views configurations:

            modelBuilder.Entity<QuestionTypeCountView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_questiontypecountsbyitembank");
            });

            modelBuilder.Entity<AutoQuestionSummaryView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_autoquestionsummary");
            });

            modelBuilder.Entity<ManualQuestionSummaryView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_manualquestionsummary");
            });

            modelBuilder.Entity<SchedulePaperView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_schedulepapers_summary");
            });

            modelBuilder.Entity<FlattenedTreeNodeView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_flattenedtreenodes");
            });

            modelBuilder.Entity<PaperStageSectionBlockSummaryView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_PaperStageSectionBlocksSummary");
            });

            modelBuilder.Entity<SchedulePaperAllocationView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_schedulepaperallocation");
            });

            modelBuilder.Entity<CandidateQuestionsWithEquationView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_CandidateQuestionsWithEquationView");
            });

            modelBuilder.Entity<PaperFormAttendanceReportView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView($"vw_{nameof(PaperFormAttendanceReportView)}");
            });

            modelBuilder.Entity<CBTSyncStatusView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("vw_cbtsyncstatus");
            });

            modelBuilder.Entity<ItemBanksFromQuestionsBlocksView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView($"vw_{nameof(ItemBanksFromQuestionsBlocksView)}");
            });

            modelBuilder.Entity<CentersSyncStatusView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView($"vw_{nameof(CentersSyncStatusView)}");
            });

            modelBuilder.Entity<SyncStatusOESToCESReportView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView($"vw_{nameof(SyncStatusOESToCESReportView)}");
            });

            modelBuilder.Entity<ItemBanksSummaryReportView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView($"vw_{nameof(ItemBanksSummaryReportView)}");
            });

            modelBuilder.Entity<PendingAnswersToEvaluate>(entity =>
            {
                entity.HasNoKey();
                entity.ToView($"vw_{nameof(PendingAnswersToEvaluate)}");

                entity.Property(e => e.QuestionText).HasConversion<SecureCompressedStringConverter>();
                entity.Property(e => e.ModelAnswer).HasConversion<SecureCompressedStringConverter>();
                entity.Property(e => e.TypedAnswerText).HasConversion<SecureCompressedStringConverter>();
            });


            // Custom indexes configurations:

            modelBuilder.ConfigureCustomIndexes();


            // Entity relations configurations:

            modelBuilder.Entity<AutoPaperItemBankQuestionSection>()
                        .HasOne(i => i.Section)
                        .WithMany(s => s.AutoQuestions)
                        .HasForeignKey(i => i.SectionId)
                        .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ScheduleMetadata>()
                        .HasOne(e => e.SecurityConfiguration)
                        .WithOne(e => e.ScheduleMetadata)
                        .OnDelete(DeleteBehavior.Restrict);

            modelBuilder
                .Entity<QuestionMetadata>()
                .HasOne(x => x.ParentSubQuestion)
                .WithMany(x => x.SubQuestions)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CandidateBatchImportHistory>()
                .Property(e => e.DataSource)
                .HasConversion<string>();

            modelBuilder.Entity<SegmentQuestionProperties>()
                .HasOne(p => p.QuestionDetails)
                .WithOne(q => q.SegmentQuestionProperties)
                .HasForeignKey<QuestionDetails>(q => q.SegmentQuestionPropertiesId);


            // Handling security and compression concerns using value converters:

            modelBuilder.Entity<QuestionDetails>(entity =>
            {
                entity.Property(e => e.Body).HasConversion<SecureCompressedStringConverter>();
                entity.Property(e => e.Instructions).HasConversion<SecureCompressedStringConverter>();
                entity.Property(e => e.ModelAnswer).HasConversion<SecureCompressedStringConverter>();
            });

            modelBuilder.Entity<QuestionDetailsVersions>(entity =>
            {
                entity.Property(e => e.Body).HasConversion<SecureCompressedStringConverter>();
                entity.Property(e => e.Instructions).HasConversion<SecureCompressedStringConverter>();
                entity.Property(e => e.ModelAnswer).HasConversion<SecureCompressedStringConverter>();
            });

            modelBuilder
                .Entity<QuestionsChoicesVersions>()
                .Property(e => e.ChoiceText)
                .HasConversion<SecureCompressedStringConverter>();

            modelBuilder
                .Entity<QuestionsChoices>()
                .Property(e => e.ChoiceText)
                .HasConversion<SecureCompressedStringConverter>();

            modelBuilder
                .Entity<CandidateQuestionsAnswers>()
                .Property(e => e.AnswerText)
                .HasConversion<SecureCompressedStringConverter>();

            modelBuilder
                .Entity<Block>()
                .Property(x => x.ConsiderDifficultyLevel)
                .HasDefaultValue(true);
        }

        public void ApplyFilter(FilterParamsValues filterParamsValues)
        {
            FilterParamsValues = filterParamsValues;
        }

        private static void SetGlobalIsDeletedFilterToAllEntities(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var isDeletedProperty = entityType.FindProperty("IsDeleted");

                if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");

                    var property = Expression.Property(parameter, "IsDeleted");

                    var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);

                    entityType.SetQueryFilter(filter);
                }
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entities = ChangeTracker.Entries().ToList();

            foreach (var entity in entities)
            {
                if (entity.State == EntityState.Added)
                {
                    if (ExplicitAudit.HasValue)
                    {
                        entity.Property("CreationDate").CurrentValue = ExplicitAudit.Value.CreationDate;

                        entity.Property("CreationUser").CurrentValue = ExplicitAudit.Value.User;

                        if (entity.Properties.Any(x => x.Metadata.Name == "OrganizationSignature"))
                            entity.Property("OrganizationSignature").CurrentValue = ExplicitAudit.Value.OrganizationSignature;

                        if (entity.Properties.Any(x => x.Metadata.Name == "OrganizationId"))
                            entity.Property("OrganizationId").CurrentValue = ExplicitAudit.Value.OrganizationId;
                    }
                    else
                    {
                        entity.Property("CreationDate").CurrentValue = DateTimeHelper.Now;

                        entity.Property("CreationUser").CurrentValue = FilterParamsValues.UserEmail ?? DefaultSystemUser.Name;

                        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
                        {
                            if (entity.Properties.Any(x => x.Metadata.Name == "OrganizationSignature"))
                                entity.Property("OrganizationSignature").CurrentValue = FilterParamsValues.Signature;

                            if (entity.Properties.Any(x => x.Metadata.Name == "OrganizationId"))
                                entity.Property("OrganizationId").CurrentValue = FilterParamsValues.OrganizationId;
                        }
                    }
                }
                else if (entity.State == EntityState.Modified)
                {
                    entity.Property("ModeficationDate").CurrentValue = DateTimeHelper.Now;

                    entity.Property("ModeficationUser").CurrentValue = FilterParamsValues.UserEmail ?? DefaultSystemUser.Name;

                    if ((bool)entity.Property("IsDeleted").CurrentValue)
                    {
                        entity.Property("DeletedDate").CurrentValue = DateTimeHelper.Now;
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}