using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddedRentalItemTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentItems_Rentals_RentalID",
                table: "EquipmentItems");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentItems_RentalID",
                table: "EquipmentItems");

            migrationBuilder.DropColumn(
                name: "RentalID",
                table: "EquipmentItems");

            migrationBuilder.CreateTable(
                name: "RentalItems",
                columns: table => new 
                {
                    RentalItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RentalID = table.Column<int>(type: "int", nullable: false),
                    EquipmentItemID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalItems", x => x.RentalItemID);
                    table.ForeignKey(
                        name: "FK_RentalItems_EquipmentItems_EquipmentItemID",
                        column: x => x.EquipmentItemID,
                        principalTable: "EquipmentItems",
                        principalColumn: "EquipmentItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RentalItems_Rentals_RentalID",
                        column: x => x.RentalID,
                        principalTable: "Rentals",
                        principalColumn: "RentalID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalItems_EquipmentItemID",
                table: "RentalItems",
                column: "EquipmentItemID");

            migrationBuilder.CreateIndex(
                name: "IX_RentalItems_RentalID",
                table: "RentalItems",
                column: "RentalID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalItems");

            migrationBuilder.AddColumn<int>(
                name: "RentalID",
                table: "EquipmentItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_RentalID",
                table: "EquipmentItems",
                column: "RentalID");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentItems_Rentals_RentalID",
                table: "EquipmentItems",
                column: "RentalID",
                principalTable: "Rentals",
                principalColumn: "RentalID");
        }
    }
}
