using DataLayer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <summary>
    /// Hand-written migration. Links SeasonPrices.ChangedByStaffID to StaffMembers so every
    /// price change points to an existing staff member. Restrict prevents deleting staff
    /// who have changed prices, so the price log stays complete.
    /// Written by hand because this branch's model snapshot is missing other migrations
    /// already applied to the shared database; Add-Migration would have included those too.
    /// </summary>
    [DbContext(typeof(SkiCenterDbContext))]
    [Migration("20261008143000_AddChangedByStaffForeignKey")]
    public partial class AddChangedByStaffForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SeasonPrices_ChangedByStaffID",
                table: "SeasonPrices",
                column: "ChangedByStaffID");

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonPrices_StaffMembers_ChangedByStaffID",
                table: "SeasonPrices",
                column: "ChangedByStaffID",
                principalTable: "StaffMembers",
                principalColumn: "StaffID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeasonPrices_StaffMembers_ChangedByStaffID",
                table: "SeasonPrices");

            migrationBuilder.DropIndex(
                name: "IX_SeasonPrices_ChangedByStaffID",
                table: "SeasonPrices");
        }
    }
}
