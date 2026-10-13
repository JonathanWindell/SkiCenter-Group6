using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonPriceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChangedByStaffID",
                table: "SeasonPrices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "SeasonPrices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2000, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangedByStaffID",
                table: "SeasonPrices");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "SeasonPrices");
        }
    }
}
