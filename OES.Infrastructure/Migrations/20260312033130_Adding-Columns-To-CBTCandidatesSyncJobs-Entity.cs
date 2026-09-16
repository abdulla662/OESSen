using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingColumnsToCBTCandidatesSyncJobsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TCIds",
                table: "Venues",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CenterCode",
                table: "CBTCandidatesSyncJobs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VenueCode",
                table: "CBTCandidatesReceivedData",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "CandidateExamTrialId",
                table: "CandidateQuestionsAnswers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CandidateExamTrialId",
                table: "BlockCandidateAnswers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ScheduleId",
                table: "BlockCandidateAnswers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SchedulePaperId",
                table: "BlockCandidateAnswers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TCIds",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "CenterCode",
                table: "CBTCandidatesSyncJobs");

            migrationBuilder.DropColumn(
                name: "VenueCode",
                table: "CBTCandidatesReceivedData");

            migrationBuilder.DropColumn(
                name: "CandidateExamTrialId",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropColumn(
                name: "CandidateExamTrialId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropColumn(
                name: "SchedulePaperId",
                table: "BlockCandidateAnswers");
        }
    }
}
