using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangingSomeKeysInResultTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockCandidateAnswers_Venues_VenueId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropIndex(
                name: "IX_Candidate_Paper_Trial_Venue_Unique",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropIndex(
                name: "IX_BlockCandidateAnswers_VenueId",
                table: "BlockCandidateAnswers");

            migrationBuilder.AlterColumn<string>(
                name: "CandidateCode",
                table: "CandidateQuestionsAnswers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Candidate_Paper_Trial_Venue_Unique",
                table: "CandidateQuestionsAnswers",
                columns: new[] { "PaperId", "PaperFormId", "VenueId", "RegistrationId", "CandidateExamTrialId", "QuestionId", "SectionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Candidate_Paper_Trial_Venue_Unique",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.AlterColumn<string>(
                name: "CandidateCode",
                table: "CandidateQuestionsAnswers",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Candidate_Paper_Trial_Venue_Unique",
                table: "CandidateQuestionsAnswers",
                columns: new[] { "CandidateCode", "PaperId", "PaperFormId", "VenueId", "TrialNumber", "QuestionId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockCandidateAnswers_VenueId",
                table: "BlockCandidateAnswers",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlockCandidateAnswers_Venues_VenueId",
                table: "BlockCandidateAnswers",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
