using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedExternallyAddedIndexesToBeFromApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateIndex(
            //    name: "IX_CTL_Candidate_ExamTrial",
            //    table: "CandidateTrackingLogs",
            //    columns: new[] { "CandidateId", "CandidateExamTrialId" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CTL_CandidateExamTrialId",
            //    table: "CandidateTrackingLogs",
            //    column: "CandidateExamTrialId");

            //migrationBuilder.CreateIndex(
            //    name: "idx_exam_qid_answered_correct",
            //    table: "CandidateQuestionsAnswers",
            //    columns: new[] { "ExamStartDate", "QuestionId", "Answered", "IsCorrect" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CQA_Candidate_Registration",
            //    table: "CandidateQuestionsAnswers",
            //    columns: new[] { "CandidateId", "RegistrationId" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CQA_CandidateExamTrialId",
            //    table: "CandidateQuestionsAnswers",
            //    column: "CandidateExamTrialId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CQA_CandidateId_PaperFormId_TrialNumber",
            //    table: "CandidateQuestionsAnswers",
            //    columns: new[] { "CandidateId", "PaperFormId", "TrialNumber" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CQA_PaperId_PaperFormId",
            //    table: "CandidateQuestionsAnswers",
            //    columns: new[] { "PaperId", "PaperFormId" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CQA_QuestionId",
            //    table: "CandidateQuestionsAnswers",
            //    column: "QuestionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CQA_ScheduleId",
            //    table: "CandidateQuestionsAnswers",
            //    column: "ScheduleId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CED_CandidateExamDate",
            //    table: "CandidateExamDetails",
            //    column: "CandidateExamDate");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CED_CandidateId_PaperFormId",
            //    table: "CandidateExamDetails",
            //    columns: new[] { "CandidateId", "PaperFormId" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_CED_ScheduleId",
            //    table: "CandidateExamDetails",
            //    column: "ScheduleId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CED_Synced",
            //    table: "CandidateExamDetails",
            //    column: "Synced");

            //migrationBuilder.CreateIndex(
            //    name: "IX_CED_UserId",
            //    table: "CandidateExamDetails",
            //    column: "UserId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_BCA_CandidateExamTrialId",
            //    table: "BlockCandidateAnswers",
            //    column: "CandidateExamTrialId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_BCA_CandidateId_PaperFormId",
            //    table: "BlockCandidateAnswers",
            //    columns: new[] { "CandidateId", "PaperFormId" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_BCA_ScheduleId",
            //    table: "BlockCandidateAnswers",
            //    column: "ScheduleId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_BCA_Synced",
            //    table: "BlockCandidateAnswers",
            //    column: "Synced");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CTL_Candidate_ExamTrial",
                table: "CandidateTrackingLogs");

            migrationBuilder.DropIndex(
                name: "IX_CTL_CandidateExamTrialId",
                table: "CandidateTrackingLogs");

            migrationBuilder.DropIndex(
                name: "idx_exam_qid_answered_correct",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_CQA_Candidate_Registration",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_CQA_CandidateExamTrialId",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_CQA_CandidateId_PaperFormId_TrialNumber",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_CQA_PaperId_PaperFormId",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_CQA_QuestionId",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_CQA_ScheduleId",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_CED_CandidateExamDate",
                table: "CandidateExamDetails");

            migrationBuilder.DropIndex(
                name: "IX_CED_CandidateId_PaperFormId",
                table: "CandidateExamDetails");

            migrationBuilder.DropIndex(
                name: "IX_CED_ScheduleId",
                table: "CandidateExamDetails");

            migrationBuilder.DropIndex(
                name: "IX_CED_Synced",
                table: "CandidateExamDetails");

            migrationBuilder.DropIndex(
                name: "IX_CED_UserId",
                table: "CandidateExamDetails");

            migrationBuilder.DropIndex(
                name: "IX_BCA_CandidateExamTrialId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropIndex(
                name: "IX_BCA_CandidateId_PaperFormId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropIndex(
                name: "IX_BCA_ScheduleId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropIndex(
                name: "IX_BCA_Synced",
                table: "BlockCandidateAnswers");
        }
    }
}
