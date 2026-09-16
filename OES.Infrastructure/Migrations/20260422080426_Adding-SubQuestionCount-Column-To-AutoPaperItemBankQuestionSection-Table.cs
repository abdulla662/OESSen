using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddingSubQuestionCountColumnToAutoPaperItemBankQuestionSectionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubQuestionCount",
                table: "AutoPaperItemBankQuestionSections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubQuestionCount",
                table: "AutoPaperItemBankQuestionSections");
        }
    }
}
