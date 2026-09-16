using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingIsDemoFlagToCandidateExamDetailsTableAndEnhancingResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_SchedulePapers_PaperId",
            //    table: "SchedulePapers");

            migrationBuilder.RenameIndex(
                name: "IX_PaperSettings_SchedulePaperId",
                table: "PaperSettings",
                newName: "idx_pss_schedulepapereid");

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "idx_sps_paperid_schedulemetaid",
                table: "SchedulePapers",
                columns: new[] { "PaperId", "ScheduleMetadataId" });

            migrationBuilder.CreateIndex(
                name: "idx_ced_examdate_formid",
                table: "CandidateExamDetails",
                columns: new[] { "CandidateExamDate", "PaperFormIdActual" });

            migrationBuilder.CreateIndex(
                name: "idx_ced_isdemo",
                table: "CandidateExamDetails",
                column: "IsDemo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_sps_paperid_schedulemetaid",
                table: "SchedulePapers");

            migrationBuilder.DropIndex(
                name: "idx_ced_examdate_formid",
                table: "CandidateExamDetails");

            migrationBuilder.DropIndex(
                name: "idx_ced_isdemo",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "CandidateExamDetails");

            migrationBuilder.RenameIndex(
                name: "idx_pss_schedulepapereid",
                table: "PaperSettings",
                newName: "IX_PaperSettings_SchedulePaperId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_SchedulePapers_PaperId",
            //    table: "SchedulePapers",
            //    column: "PaperId");
        }
    }
}
