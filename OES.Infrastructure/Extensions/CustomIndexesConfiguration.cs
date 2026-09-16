using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Schedule;

namespace OES.Infrastructure.Extensions
{
    public static class CustomIndexesConfiguration
    {
        public static void ConfigureCustomIndexes(this ModelBuilder modelBuilder)
        {
            // 1. BlockCandidateAnswers
            modelBuilder.Entity<BlockCandidateAnswer>(entity =>
            {
                entity.HasIndex(e => e.CandidateExamTrialId)
                      .HasDatabaseName("IX_BCA_CandidateExamTrialId");

                entity.HasIndex(e => new { e.CandidateId, e.PaperFormId })
                      .HasDatabaseName("IX_BCA_CandidateId_PaperFormId");

                entity.HasIndex(e => e.ScheduleId)
                      .HasDatabaseName("IX_BCA_ScheduleId");

                entity.HasIndex(e => e.Synced)
                      .HasDatabaseName("IX_BCA_Synced");
            });

            // 2. CandidateExamDetails
            modelBuilder.Entity<CandidateExamDetails>(entity =>
            {
                entity.HasIndex(e => new { e.CandidateExamDate, e.PaperFormIdActual })
                      .HasDatabaseName("idx_ced_examdate_formid");

                entity.HasIndex(e => e.IsDemo)
                      .HasDatabaseName("idx_ced_isdemo");

                entity.HasIndex(e => e.CandidateExamDate)
                      .HasDatabaseName("IX_CED_CandidateExamDate");

                entity.HasIndex(e => new { e.CandidateId, e.PaperFormId })
                      .HasDatabaseName("IX_CED_CandidateId_PaperFormId");

                entity.HasIndex(e => e.ScheduleId)
                      .HasDatabaseName("IX_CED_ScheduleId");

                entity.HasIndex(e => e.Synced)
                      .HasDatabaseName("IX_CED_Synced");

                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_CED_UserId");
            });

            // 3. CandidateQuestionsAnswers
            modelBuilder.Entity<CandidateQuestionsAnswers>(entity =>
            {
                entity.HasIndex(e => new { e.RegistrationId, e.EvaluationStatus, e.IsDeleted })
                      .HasDatabaseName("idx_cqa_reg_eval_status");

                entity.HasIndex(e => new { e.ExamStartDate, e.QuestionId, e.Answered, e.IsCorrect })
                      .HasDatabaseName("idx_exam_qid_answered_correct");

                entity.HasIndex(e => new { e.CandidateId, e.RegistrationId })
                      .HasDatabaseName("IX_CQA_Candidate_Registration");

                entity.HasIndex(e => e.CandidateExamTrialId)
                      .HasDatabaseName("IX_CQA_CandidateExamTrialId");

                entity.HasIndex(e => new { e.CandidateId, e.PaperFormId, e.TrialNumber })
                      .HasDatabaseName("IX_CQA_CandidateId_PaperFormId_TrialNumber");

                entity.HasIndex(e => new { e.PaperId, e.PaperFormId })
                      .HasDatabaseName("IX_CQA_PaperId_PaperFormId");

                entity.HasIndex(e => e.QuestionId)
                      .HasDatabaseName("IX_CQA_QuestionId");

                entity.HasIndex(e => e.ScheduleId)
                      .HasDatabaseName("IX_CQA_ScheduleId");

                //entity.HasIndex(e => new
                //{
                //    e.PaperId,
                //    e.PaperFormId,
                //    e.VenueId,
                //    e.RegistrationId,
                //    e.CandidateExamTrialId,
                //    e.QuestionId,
                //    e.SectionId
                //})
                //.HasDatabaseName("IX_Candidate_Paper_Trial_Venue_Unique")
                //.IsUnique()
                //.HasFilter(null);
            });

            // 4. Candidates
            modelBuilder.Entity<Candidate>(entity =>
            {
                entity.HasIndex(i => new { i.Id, i.OrganizationSignature, i.OrganizationId })
                      .IsUnique();

                entity.HasIndex(c => new { c.Email, c.OrganizationId })
                      .IsUnique()
                      .HasDatabaseName("IX_Candidates_Org_Email")
                      .HasFilter(null);

                entity.Property(c => c.Email)
                      .HasMaxLength(255);
            });

            // 5. CandidateOrganizationNodeLookupItem
            modelBuilder.Entity<CandidateOrganizationNodeLookupItem>(entity =>
            {
                entity.HasIndex(cl => new { cl.CandidateId, cl.OrganizationNodeLookupItemId, cl.OrganizationId })
                      .IsUnique()
                      .HasDatabaseName("IX_CandidateLookup_Unique");
            });

            // 6. CandidateTrackingLog
            modelBuilder.Entity<CandidateTrackingLog>(entity =>
            {
                entity.HasIndex(e => new { e.CandidateId, e.CandidateExamTrialId })
                      .HasDatabaseName("IX_CTL_Candidate_ExamTrial");

                entity.HasIndex(e => e.CandidateExamTrialId)
                      .HasDatabaseName("IX_CTL_CandidateExamTrialId");
            });

            // 7. SchedulePaperSettings
            modelBuilder.Entity<SchedulePaperSettings>(entity =>
            {
                entity.HasIndex(e => e.SchedulePaperId)
                      .HasDatabaseName("idx_pss_schedulepapereid");
            });

            // 8. SchedulePaper
            modelBuilder.Entity<SchedulePaper>(entity =>
            {
                entity.HasIndex(e => new { e.PaperId, e.ScheduleMetadataId })
                      .HasDatabaseName("idx_sps_paperid_schedulemetaid");
            });

            // 9. SchedulePaperCandidate
            modelBuilder.Entity<SchedulePaperCandidate>(entity =>
            {
                entity.HasIndex(i => new { i.CandidateId, i.SchedulePaperId, i.VenueId, i.OrganizationId, i.RegistrationNumber })
                      .IsUnique()
                      .HasDatabaseName("IX_SchedulePapersCandidates_Unique");
            });
        }
    }
}
