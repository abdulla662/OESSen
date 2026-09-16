using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingQuestionDeltaChangeAuditingFieldsToQuestionMetadataEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeltaUpdatedFromExcelBy",
                table: "QuestionsMetaData",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDeltaUpdatedFromExcel",
                table: "QuestionsMetaData",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeltaUpdatedFromExcelBy",
                table: "QuestionsMetaData");

            migrationBuilder.DropColumn(
                name: "LastDeltaUpdatedFromExcel",
                table: "QuestionsMetaData");
        }
    }
}
