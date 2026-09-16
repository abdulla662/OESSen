using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DoSomeChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_cqa_reg_eval_status",
                table: "CandidateQuestionsAnswers",
                columns: new[] { "RegistrationId", "EvaluationStatus", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_cqa_reg_eval_status",
                table: "CandidateQuestionsAnswers");
        }
    }
}
