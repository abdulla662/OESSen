using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakingSomeSchedulePaperSettingsTemplatesNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_CertificateTemplateId",
                table: "PaperSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_OtherInstructionTemplateId",
                table: "PaperSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_ResultTemplateId",
                table: "PaperSettings");

            migrationBuilder.AlterColumn<long>(
                name: "ResultTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "OtherInstructionTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "CertificateTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_CertificateTemplateId",
                table: "PaperSettings",
                column: "CertificateTemplateId",
                principalTable: "Templates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_OtherInstructionTemplateId",
                table: "PaperSettings",
                column: "OtherInstructionTemplateId",
                principalTable: "Templates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_ResultTemplateId",
                table: "PaperSettings",
                column: "ResultTemplateId",
                principalTable: "Templates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_CertificateTemplateId",
                table: "PaperSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_OtherInstructionTemplateId",
                table: "PaperSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_PaperSettings_Templates_ResultTemplateId",
                table: "PaperSettings");

            migrationBuilder.AlterColumn<long>(
                name: "ResultTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "OtherInstructionTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "CertificateTemplateId",
                table: "PaperSettings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_CertificateTemplateId",
                table: "PaperSettings",
                column: "CertificateTemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_OtherInstructionTemplateId",
                table: "PaperSettings",
                column: "OtherInstructionTemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaperSettings_Templates_ResultTemplateId",
                table: "PaperSettings",
                column: "ResultTemplateId",
                principalTable: "Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
