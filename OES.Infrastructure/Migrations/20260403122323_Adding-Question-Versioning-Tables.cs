using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingQuestionVersioningTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReplaced",
                table: "QuestionsMetaData",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "QuestionDetailsVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    QuestionDetailsId = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instructions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModelAnswer = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionMetadataId = table.Column<long>(type: "bigint", nullable: false),
                    LanguageId = table.Column<long>(type: "bigint", nullable: false),
                    HasShuffled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxWords = table.Column<long>(type: "bigint", nullable: true),
                    MaxRecordingTimeInSeconds = table.Column<int>(type: "int", nullable: false),
                    UseArabicNumbers = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationUser = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeficationUser = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModeficationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OrganizationSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionDetailsVersions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QuestionMetadataVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    QuestionMetaId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Delta = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MaximumAnswerTime = table.Column<int>(type: "int", nullable: false),
                    IsRoot = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Author = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionStatus = table.Column<int>(type: "int", nullable: false),
                    ScientificEditorPanelEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FileManagerEditorPanelEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    QuestionTypeId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionsExhaustionCount = table.Column<long>(type: "bigint", nullable: false),
                    CurrentExhaustionCount = table.Column<long>(type: "bigint", nullable: false),
                    SubjectId = table.Column<long>(type: "bigint", nullable: true),
                    IloId = table.Column<long>(type: "bigint", nullable: true),
                    ItemBankId = table.Column<long>(type: "bigint", nullable: false),
                    DifficultyProfileId = table.Column<long>(type: "bigint", nullable: false),
                    DifficultyLevelId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionLayoutId = table.Column<long>(type: "bigint", nullable: true),
                    CreationUser = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeficationUser = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModeficationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OrganizationSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionMetadataVersions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QuestionsChoicesVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    ChoiceText = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCorrectAnswer = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionDetailsId = table.Column<long>(type: "bigint", nullable: false),
                    CreationUser = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModeficationUser = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModeficationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OrganizationSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionsChoicesVersions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionDetailsVersions");

            migrationBuilder.DropTable(
                name: "QuestionMetadataVersions");

            migrationBuilder.DropTable(
                name: "QuestionsChoicesVersions");

            migrationBuilder.DropColumn(
                name: "IsReplaced",
                table: "QuestionsMetaData");
        }
    }
}
