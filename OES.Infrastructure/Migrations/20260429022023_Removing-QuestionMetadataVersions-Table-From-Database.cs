using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovingQuestionMetadataVersionsTableFromDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionMetadataVersions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionMetadataVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Author = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreationUser = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentExhaustionCount = table.Column<long>(type: "bigint", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Delta = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DifficultyLevelId = table.Column<long>(type: "bigint", nullable: false),
                    DifficultyProfileId = table.Column<long>(type: "bigint", nullable: false),
                    FileManagerEditorPanelEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IloId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsRoot = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ItemBankId = table.Column<long>(type: "bigint", nullable: false),
                    MaximumAnswerTime = table.Column<int>(type: "int", nullable: false),
                    ModeficationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModeficationUser = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    QuestionCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionLayoutId = table.Column<long>(type: "bigint", nullable: true),
                    QuestionMetaId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionStatus = table.Column<int>(type: "int", nullable: false),
                    QuestionTypeId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionsExhaustionCount = table.Column<long>(type: "bigint", nullable: false),
                    ScientificEditorPanelEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubjectId = table.Column<long>(type: "bigint", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionMetadataVersions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
