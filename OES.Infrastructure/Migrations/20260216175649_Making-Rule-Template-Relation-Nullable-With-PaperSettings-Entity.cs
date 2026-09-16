using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakingRuleTemplateRelationNullableWithPaperSettingsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_RuleTemplateId",
                table: "PaperSettings");

            migrationBuilder.AlterColumn<long>(
                name: "RuleTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_RuleTemplateId",
                table: "PaperSettings",
                column: "RuleTemplateId",
                principalTable: "Templates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_RuleTemplateId",
                table: "PaperSettings");

            migrationBuilder.AlterColumn<long>(
                name: "RuleTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_RuleTemplateId",
                table: "PaperSettings",
                column: "RuleTemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
