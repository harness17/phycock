using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phycock.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthRecordCustomTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HealthRecord_UserId_RecordDate_RecordTiming",
                table: "HealthRecord");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "RecordTime",
                table: "HealthRecord",
                type: "time",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecord_UserId_RecordDate_RecordTiming_RecordTime",
                table: "HealthRecord",
                columns: new[] { "UserId", "RecordDate", "RecordTiming", "RecordTime" },
                unique: true,
                filter: "[DelFlag] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HealthRecord_UserId_RecordDate_RecordTiming_RecordTime",
                table: "HealthRecord");

            migrationBuilder.DropColumn(
                name: "RecordTime",
                table: "HealthRecord");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecord_UserId_RecordDate_RecordTiming",
                table: "HealthRecord",
                columns: new[] { "UserId", "RecordDate", "RecordTiming" },
                unique: true,
                filter: "[DelFlag] = 0");
        }
    }
}
