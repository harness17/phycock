using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phycock.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthRecordTimingUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_HealthRecord_UserId_RecordDate_RecordTiming",
                table: "HealthRecord",
                columns: new[] { "UserId", "RecordDate", "RecordTiming" },
                unique: true,
                filter: "[DelFlag] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HealthRecord_UserId_RecordDate_RecordTiming",
                table: "HealthRecord");
        }
    }
}
