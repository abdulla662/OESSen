using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OES.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedMaxSizeInMBToBeMaxSizeInKBInMediaSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxSizeInMB",
                table: "MediaSettings",
                newName: "MaxSizeInKB");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MaxSizeInKB",
                table: "MediaSettings",
                newName: "MaxSizeInMB");
        }
    }
}
