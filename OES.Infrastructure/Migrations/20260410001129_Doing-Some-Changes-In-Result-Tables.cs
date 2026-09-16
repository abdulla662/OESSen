using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DoingSomeChangesInResultTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "RegistrationId",
                table: "CandidateQuestionsAnswers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "RegistrationId",
                table: "CandidateExamDetails",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "VenueCode",
                table: "BlockCandidateAnswers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "VenueId",
                table: "BlockCandidateAnswers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlockCandidateAnswers_Venues_VenueId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropIndex(
                name: "IX_BlockCandidateAnswers_VenueId",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropColumn(
                name: "RegistrationId",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "VenueCode",
                table: "BlockCandidateAnswers");

            migrationBuilder.DropColumn(
                name: "VenueId",
                table: "BlockCandidateAnswers");

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationId",
                table: "CandidateQuestionsAnswers",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
