using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phycock.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodReflection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeriodReflection",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SelfEvaluation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Burden = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Improvement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Appetite = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Sleep = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DelFlag = table.Column<bool>(type: "bit", nullable: false),
                    UpdateApplicationUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateApplicationUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodReflection", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodReflection_UserId_PeriodType_PeriodStart",
                table: "PeriodReflection",
                columns: new[] { "UserId", "PeriodType", "PeriodStart" },
                unique: true,
                filter: "[DelFlag] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeriodReflection");
        }
    }
}
