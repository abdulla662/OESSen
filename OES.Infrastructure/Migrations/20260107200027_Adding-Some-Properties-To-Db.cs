using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingSomePropertiesToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "QuestionsChoices",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<long>(
                name: "CandidateDetailsId",
                table: "CandidateExamDetails",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<long>(
                name: "LanguageId",
                table: "Blocks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_LanguageId",
                table: "Blocks",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Languages_LanguageId",
                table: "Blocks",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Languages_LanguageId",
                table: "Blocks");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_LanguageId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "QuestionsChoices");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Blocks");

            migrationBuilder.AlterColumn<int>(
                name: "CandidateDetailsId",
                table: "CandidateExamDetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
