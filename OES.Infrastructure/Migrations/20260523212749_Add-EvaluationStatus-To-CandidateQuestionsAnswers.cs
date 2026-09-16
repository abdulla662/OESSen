using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationStatusToCandidateQuestionsAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EvaluationStatus",
                table: "CandidateQuestionsAnswers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EvaluationSyncDate",
                table: "CandidateQuestionsAnswers",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvaluationStatus",
                table: "CandidateQuestionsAnswers");

            migrationBuilder.DropColumn(
                name: "EvaluationSyncDate",
                table: "CandidateQuestionsAnswers");
        }
    }
}
