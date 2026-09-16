using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefiningCandidateExamDetailsEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CacheCreatedAt",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CacheEntryId",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CacheExpiry",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CacheKey",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CacheLastModifiedAt",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CachedPaperDuration",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CachedTimeRemaining",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateAddress",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateClickedEndExamButton",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateDateOfBirth",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateDetailsId",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateGender",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateIsActive",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateIsDeleted",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidatePaperCreatedAt",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidatePaperIsActive",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidatePaperIsDeleted",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidatePhotoUrl",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateQualification",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateRegistrationDateTime",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateSignatureUrl",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateUserId",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CandidateUserName",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CurrentQuestionIndex",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "CurrentSectionIndex",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ExamQuestionsJSON",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ExamTrackingData",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ExamTrialCreatedAt",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "NotepadContent",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "PaperAbbreviation",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "PaperEndTime",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "PaperFormIsActive",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "PaperId",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "PaperIsActive",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "PaperStartTime",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleDescription",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleEndDate",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleEndTime",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleIdActual",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleIsActive",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleIsDeleted",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleStartDate",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "ScheduleStartTime",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "SectionsJSON",
                table: "CandidateExamDetails");

            migrationBuilder.DropColumn(
                name: "TotalQuestionsInCache",
                table: "CandidateExamDetails");

            migrationBuilder.AlterColumn<long>(
                name: "UserId",
                table: "CandidateExamDetails",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CandidateExamDetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<DateTime>(
                name: "CacheCreatedAt",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CacheEntryId",
                table: "CandidateExamDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CacheExpiry",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CacheKey",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CacheLastModifiedAt",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CachedPaperDuration",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CachedTimeRemaining",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CandidateAddress",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "CandidateClickedEndExamButton",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandidateDateOfBirth",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CandidateDetailsId",
                table: "CandidateExamDetails",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "CandidateGender",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "CandidateIsActive",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CandidateIsDeleted",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandidatePaperCreatedAt",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CandidatePaperIsActive",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CandidatePaperIsDeleted",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CandidatePhotoUrl",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CandidateQualification",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CandidateRegistrationDateTime",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CandidateSignatureUrl",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "CandidateUserId",
                table: "CandidateExamDetails",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "CandidateUserName",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CurrentQuestionIndex",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CurrentSectionIndex",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExamQuestionsJSON",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExamTrackingData",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExamTrialCreatedAt",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotepadContent",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaperAbbreviation",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PaperEndTime",
                table: "CandidateExamDetails",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaperFormIsActive",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaperId",
                table: "CandidateExamDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PaperIsActive",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "PaperStartTime",
                table: "CandidateExamDetails",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleDescription",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleEndDate",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduleEndTime",
                table: "CandidateExamDetails",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleIdActual",
                table: "CandidateExamDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduleIsActive",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScheduleIsDeleted",
                table: "CandidateExamDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleStartDate",
                table: "CandidateExamDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduleStartTime",
                table: "CandidateExamDetails",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionsJSON",
                table: "CandidateExamDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestionsInCache",
                table: "CandidateExamDetails",
                type: "int",
                nullable: true);
        }
    }
}
