using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingSegmentAndMatchingPairVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SegmentQuestionPropertiesVersionsId",
                table: "QuestionDetailsVersions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchingPairQuestionItemsVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VersionNumber = table.Column<long>(type: "bigint", nullable: false),
                    Body = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColumnOrder = table.Column<int>(type: "int", nullable: false),
                    IsDataSource = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_MatchingPairQuestionItemsVersions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SegmentQuestionPropertiesVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ThinkingTime = table.Column<long>(type: "bigint", nullable: true),
                    ResponseTime = table.Column<long>(type: "bigint", nullable: true),
                    WordsCount = table.Column<long>(type: "bigint", nullable: true),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false),
                    HasScore = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SegmentAudioUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SegmentQuestionResponseType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SegmentQuestionPropertiesVersions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionDetailsVersions_SegmentQuestionPropertiesVersionsId",
                table: "QuestionDetailsVersions",
                column: "SegmentQuestionPropertiesVersionsId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionDetailsVersions_SegmentQuestionPropertiesVersions_Se~",
                table: "QuestionDetailsVersions",
                column: "SegmentQuestionPropertiesVersionsId",
                principalTable: "SegmentQuestionPropertiesVersions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionDetailsVersions_SegmentQuestionPropertiesVersions_Se~",
                table: "QuestionDetailsVersions");

            migrationBuilder.DropTable(
                name: "MatchingPairQuestionItemsVersions");

            migrationBuilder.DropTable(
                name: "SegmentQuestionPropertiesVersions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionDetailsVersions_SegmentQuestionPropertiesVersionsId",
                table: "QuestionDetailsVersions");

            migrationBuilder.DropColumn(
                name: "SegmentQuestionPropertiesVersionsId",
                table: "QuestionDetailsVersions");
        }
    }
}
