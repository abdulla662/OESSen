using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DoingSomeUpdatesInAdaptiveTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StageCategoryDecisionPaths");

            migrationBuilder.AddColumn<string>(
                name: "RenderedPartName",
                table: "Stages",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AdaptiveCategoryExecutionOrder",
                table: "PaperMetadata",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DPathCalculationMode",
                table: "PaperMetadata",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PaperStageCategoryDecisionPaths",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StageId = table.Column<long>(type: "bigint", nullable: true),
                    PaperId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    DecisionPathValue = table.Column<double>(type: "double", nullable: true),
                    FixedDPath = table.Column<double>(type: "double", nullable: true),
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
                    table.PrimaryKey("PK_PaperStageCategoryDecisionPaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaperStageCategoryDecisionPaths_PaperMetadata_PaperId",
                        column: x => x.PaperId,
                        principalTable: "PaperMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaperStageCategoryDecisionPaths_QuestionCategories_QuestionC~",
                        column: x => x.QuestionCategoryId,
                        principalTable: "QuestionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaperStageCategoryDecisionPaths_Stages_StageId",
                        column: x => x.StageId,
                        principalTable: "Stages",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PaperStageCategoryDecisionPaths_PaperId",
                table: "PaperStageCategoryDecisionPaths",
                column: "PaperId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperStageCategoryDecisionPaths_QuestionCategoryId",
                table: "PaperStageCategoryDecisionPaths",
                column: "QuestionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperStageCategoryDecisionPaths_StageId",
                table: "PaperStageCategoryDecisionPaths",
                column: "StageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaperStageCategoryDecisionPaths");

            migrationBuilder.DropColumn(
                name: "RenderedPartName",
                table: "Stages");

            migrationBuilder.DropColumn(
                name: "AdaptiveCategoryExecutionOrder",
                table: "PaperMetadata");

            migrationBuilder.DropColumn(
                name: "DPathCalculationMode",
                table: "PaperMetadata");

            migrationBuilder.CreateTable(
                name: "StageCategoryDecisionPaths",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    QuestionCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    StageId = table.Column<long>(type: "bigint", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreationUser = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DecisionPathValue = table.Column<double>(type: "double", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ModeficationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ModeficationUser = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationSignature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageCategoryDecisionPaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StageCategoryDecisionPaths_QuestionCategories_QuestionCatego~",
                        column: x => x.QuestionCategoryId,
                        principalTable: "QuestionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StageCategoryDecisionPaths_Stages_StageId",
                        column: x => x.StageId,
                        principalTable: "Stages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StageCategoryDecisionPaths_QuestionCategoryId",
                table: "StageCategoryDecisionPaths",
                column: "QuestionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StageCategoryDecisionPaths_StageId",
                table: "StageCategoryDecisionPaths",
                column: "StageId");
        }
    }
}
