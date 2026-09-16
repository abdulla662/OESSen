using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleMatrixNewGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SchedulePaperCandidateId",
                table: "ScheduleGroups",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CandidateGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CandidateId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_CandidateGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateGroups_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CandidateGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeltaTypeGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DeltaTypeId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_DeltaTypeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeltaTypeGroups_DeltaTypes_DeltaTypeId",
                        column: x => x.DeltaTypeId,
                        principalTable: "DeltaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeltaTypeGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DifficultyLevelGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DifficultyLevelId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_DifficultyLevelGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DifficultyLevelGroups_DifficultyLevels_DifficultyLevelId",
                        column: x => x.DifficultyLevelId,
                        principalTable: "DifficultyLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DifficultyLevelGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DifficultyProfileGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DifficultyProfileId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_DifficultyProfileGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DifficultyProfileGroups_DifficultyProfiles_DifficultyProfile~",
                        column: x => x.DifficultyProfileId,
                        principalTable: "DifficultyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DifficultyProfileGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquationGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EquationId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_EquationGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquationGroups_EquationTemplates_EquationId",
                        column: x => x.EquationId,
                        principalTable: "EquationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquationGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LanguageGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LanguageId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_LanguageGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanguageGroups_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LanguageGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaSettingsGroup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MediaConfigurationId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_MediaSettingsGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaSettingsGroup_MediaSettings_MediaConfigurationId",
                        column: x => x.MediaConfigurationId,
                        principalTable: "MediaSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaSettingsGroup_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QualityCheckCommitteeGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QualityCheckCommitteeId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_QualityCheckCommitteeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityCheckCommitteeGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QualityCheckCommitteeGroups_QualityCheckCommittees_QualityCh~",
                        column: x => x.QualityCheckCommitteeId,
                        principalTable: "QualityCheckCommittees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "QuestionCategoryGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QuestionCategoryId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_QuestionCategoryGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionCategoryGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionCategoryGroups_QuestionCategories_QuestionCategoryId",
                        column: x => x.QuestionCategoryId,
                        principalTable: "QuestionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SubjectGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SubjectId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_SubjectGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectGroups_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TransitionProfileGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_TransitionProfileGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransitionProfileGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransitionProfileGroups_TransitionProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "TransitionProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VenueGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OESGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VenueId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_VenueGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueGroups_OESGroups_OESGroupId",
                        column: x => x.OESGroupId,
                        principalTable: "OESGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenueGroups_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleGroups_SchedulePaperCandidateId",
                table: "ScheduleGroups",
                column: "SchedulePaperCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateGroups_CandidateId",
                table: "CandidateGroups",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateGroups_OESGroupId",
                table: "CandidateGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DeltaTypeGroups_DeltaTypeId",
                table: "DeltaTypeGroups",
                column: "DeltaTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DeltaTypeGroups_OESGroupId",
                table: "DeltaTypeGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DifficultyLevelGroups_DifficultyLevelId",
                table: "DifficultyLevelGroups",
                column: "DifficultyLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_DifficultyLevelGroups_OESGroupId",
                table: "DifficultyLevelGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DifficultyProfileGroups_DifficultyProfileId",
                table: "DifficultyProfileGroups",
                column: "DifficultyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DifficultyProfileGroups_OESGroupId",
                table: "DifficultyProfileGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EquationGroups_EquationId",
                table: "EquationGroups",
                column: "EquationId");

            migrationBuilder.CreateIndex(
                name: "IX_EquationGroups_OESGroupId",
                table: "EquationGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageGroups_LanguageId",
                table: "LanguageGroups",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_LanguageGroups_OESGroupId",
                table: "LanguageGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaSettingsGroup_MediaConfigurationId",
                table: "MediaSettingsGroup",
                column: "MediaConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaSettingsGroup_OESGroupId",
                table: "MediaSettingsGroup",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityCheckCommitteeGroups_OESGroupId",
                table: "QualityCheckCommitteeGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityCheckCommitteeGroups_QualityCheckCommitteeId",
                table: "QualityCheckCommitteeGroups",
                column: "QualityCheckCommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategoryGroups_OESGroupId",
                table: "QuestionCategoryGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategoryGroups_QuestionCategoryId",
                table: "QuestionCategoryGroups",
                column: "QuestionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGroups_OESGroupId",
                table: "SubjectGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectGroups_SubjectId",
                table: "SubjectGroups",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TransitionProfileGroups_OESGroupId",
                table: "TransitionProfileGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TransitionProfileGroups_ProfileId",
                table: "TransitionProfileGroups",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueGroups_OESGroupId",
                table: "VenueGroups",
                column: "OESGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueGroups_VenueId",
                table: "VenueGroups",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleGroups_SchedulePapersCandidates_SchedulePaperCandida~",
                table: "ScheduleGroups",
                column: "SchedulePaperCandidateId",
                principalTable: "SchedulePapersCandidates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleGroups_SchedulePapersCandidates_SchedulePaperCandida~",
                table: "ScheduleGroups");

            migrationBuilder.DropTable(
                name: "CandidateGroups");

            migrationBuilder.DropTable(
                name: "DeltaTypeGroups");

            migrationBuilder.DropTable(
                name: "DifficultyLevelGroups");

            migrationBuilder.DropTable(
                name: "DifficultyProfileGroups");

            migrationBuilder.DropTable(
                name: "EquationGroups");

            migrationBuilder.DropTable(
                name: "LanguageGroups");

            migrationBuilder.DropTable(
                name: "MediaSettingsGroup");

            migrationBuilder.DropTable(
                name: "QualityCheckCommitteeGroups");

            migrationBuilder.DropTable(
                name: "QuestionCategoryGroups");

            migrationBuilder.DropTable(
                name: "SubjectGroups");

            migrationBuilder.DropTable(
                name: "TransitionProfileGroups");

            migrationBuilder.DropTable(
                name: "VenueGroups");

            migrationBuilder.DropIndex(
                name: "IX_ScheduleGroups_SchedulePaperCandidateId",
                table: "ScheduleGroups");

            migrationBuilder.DropColumn(
                name: "SchedulePaperCandidateId",
                table: "ScheduleGroups");
        }
    }
}
