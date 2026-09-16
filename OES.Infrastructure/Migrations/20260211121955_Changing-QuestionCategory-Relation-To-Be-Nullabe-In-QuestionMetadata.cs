using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangingQuestionCategoryRelationToBeNullabeInQuestionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionsMetaData_QuestionCategories_QuestionCategoryId",
                table: "QuestionsMetaData");

            migrationBuilder.AlterColumn<long>(
                name: "QuestionCategoryId",
                table: "QuestionsMetaData",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionsMetaData_QuestionCategories_QuestionCategoryId",
                table: "QuestionsMetaData",
                column: "QuestionCategoryId",
                principalTable: "QuestionCategories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionsMetaData_QuestionCategories_QuestionCategoryId",
                table: "QuestionsMetaData");

            migrationBuilder.AlterColumn<long>(
                name: "QuestionCategoryId",
                table: "QuestionsMetaData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionsMetaData_QuestionCategories_QuestionCategoryId",
                table: "QuestionsMetaData",
                column: "QuestionCategoryId",
                principalTable: "QuestionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
