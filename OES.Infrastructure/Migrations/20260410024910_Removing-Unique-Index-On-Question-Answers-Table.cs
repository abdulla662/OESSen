using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovingUniqueIndexOnQuestionAnswersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Candidate_Paper_Trial_Venue_Unique",
                table: "CandidateQuestionsAnswers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Candidate_Paper_Trial_Venue_Unique",
                table: "CandidateQuestionsAnswers",
                columns: new[] { "PaperId", "PaperFormId", "VenueId", "RegistrationId", "CandidateExamTrialId", "QuestionId", "SectionId" },
                unique: true);
        }
    }
}
