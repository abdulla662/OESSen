using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedStepSegmentQuestionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SegmentQuestionPropertiesId",
                table: "QuestionsDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SegmentQuestionProperties",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ThinkingTime = table.Column<long>(type: "bigint", nullable: true),
                    ResponseTime = table.Column<long>(type: "bigint", nullable: true),
                    WordsCount = table.Column<long>(type: "bigint", nullable: true),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false),
                    HasScore = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_SegmentQuestionProperties", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionsDetails_SegmentQuestionPropertiesId",
                table: "QuestionsDetails",
                column: "SegmentQuestionPropertiesId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionsDetails_SegmentQuestionProperties_SegmentQuestionPr~",
                table: "QuestionsDetails",
                column: "SegmentQuestionPropertiesId",
                principalTable: "SegmentQuestionProperties",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionsDetails_SegmentQuestionProperties_SegmentQuestionPr~",
                table: "QuestionsDetails");

            migrationBuilder.DropTable(
                name: "SegmentQuestionProperties");

            migrationBuilder.DropIndex(
                name: "IX_QuestionsDetails_SegmentQuestionPropertiesId",
                table: "QuestionsDetails");

            migrationBuilder.DropColumn(
                name: "SegmentQuestionPropertiesId",
                table: "QuestionsDetails");
        }
    }
}
