using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingOrderIdToQuestionChoiceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "QuestionsChoices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "QuestionsChoices");
        }
    }
}
