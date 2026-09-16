using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingVenueIdToCandidateExamDetailsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "VenueId",
                table: "CandidateExamDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateExamDetails_VenueId",
                table: "CandidateExamDetails",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateExamDetails_Venues_VenueId",
                table: "CandidateExamDetails",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateExamDetails_Venues_VenueId",
                table: "CandidateExamDetails");

            migrationBuilder.DropIndex(
                name: "IX_CandidateExamDetails_VenueId",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "VenueId",
                table: "CandidateExamDetails");
        }
    }
}
