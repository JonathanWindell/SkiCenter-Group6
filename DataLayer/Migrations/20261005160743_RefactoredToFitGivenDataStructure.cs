using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class RefactoredToFitGivenDataStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_SeasonPrices_SeasonPriceID",
                table: "Accommodations");

            migrationBuilder.DropTable(
                name: "EquipmentItemRental");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "SkiLessons");

            migrationBuilder.DropColumn(
                name: "WeekDay",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "PricePerDay",
                table: "EquipmentItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "NumberOfBeds",
                table: "Accommodations");

            migrationBuilder.RenameColumn(
                name: "UnitName",
                table: "Accommodations",
                newName: "AccommodationNumber");

            migrationBuilder.RenameColumn(
                name: "SeasonPriceID",
                table: "Accommodations",
                newName: "AccommodationTypeID");

            migrationBuilder.RenameIndex(
                name: "IX_Accommodations_SeasonPriceID",
                table: "Accommodations",
                newName: "IX_Accommodations_AccommodationTypeID");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "SkiLessonSession",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "AccommodationTypeID",
                table: "SeasonPrices",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "MeetingRooms",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "MeetingRooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MeetingRoomNumber",
                table: "MeetingRooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "MeetingRooms",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RentalID",
                table: "EquipmentItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Accommodations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "AccommodationType",
                columns: table => new
                {
                    AccommodationTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfBeds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccommodationType", x => x.AccommodationTypeID);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentPriceMatrix",
                columns: table => new
                {
                    EquipmentPriceMatrixID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Days = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<int>(type: "int", nullable: false),
                    EquipmentID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentPriceMatrix", x => x.EquipmentPriceMatrixID);
                    table.ForeignKey(
                        name: "FK_EquipmentPriceMatrix_Equipment_EquipmentID",
                        column: x => x.EquipmentID,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonPrices_AccommodationTypeID",
                table: "SeasonPrices",
                column: "AccommodationTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItems_RentalID",
                table: "EquipmentItems",
                column: "RentalID");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentPriceMatrix_EquipmentID",
                table: "EquipmentPriceMatrix",
                column: "EquipmentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_AccommodationType_AccommodationTypeID",
                table: "Accommodations",
                column: "AccommodationTypeID",
                principalTable: "AccommodationType",
                principalColumn: "AccommodationTypeID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentItems_Rentals_RentalID",
                table: "EquipmentItems",
                column: "RentalID",
                principalTable: "Rentals",
                principalColumn: "RentalID");

            migrationBuilder.AddForeignKey(
                name: "FK_SeasonPrices_AccommodationType_AccommodationTypeID",
                table: "SeasonPrices",
                column: "AccommodationTypeID",
                principalTable: "AccommodationType",
                principalColumn: "AccommodationTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_AccommodationType_AccommodationTypeID",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentItems_Rentals_RentalID",
                table: "EquipmentItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SeasonPrices_AccommodationType_AccommodationTypeID",
                table: "SeasonPrices");

            migrationBuilder.DropTable(
                name: "AccommodationType");

            migrationBuilder.DropTable(
                name: "EquipmentPriceMatrix");

            migrationBuilder.DropIndex(
                name: "IX_SeasonPrices_AccommodationTypeID",
                table: "SeasonPrices");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentItems_RentalID",
                table: "EquipmentItems");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "SkiLessonSession");

            migrationBuilder.DropColumn(
                name: "AccommodationTypeID",
                table: "SeasonPrices");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "MeetingRoomNumber",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "RentalID",
                table: "EquipmentItems");

            migrationBuilder.RenameColumn(
                name: "AccommodationTypeID",
                table: "Accommodations",
                newName: "SeasonPriceID");

            migrationBuilder.RenameColumn(
                name: "AccommodationNumber",
                table: "Accommodations",
                newName: "UnitName");

            migrationBuilder.RenameIndex(
                name: "IX_Accommodations_AccommodationTypeID",
                table: "Accommodations",
                newName: "IX_Accommodations_SeasonPriceID");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "SkiLessons",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MeetingRooms",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "WeekDay",
                table: "MeetingRooms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerDay",
                table: "EquipmentItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Accommodations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Accommodations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Accommodations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBeds",
                table: "Accommodations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EquipmentItemRental",
                columns: table => new
                {
                    EquipmentItemsEquipmentItemID = table.Column<int>(type: "int", nullable: false),
                    RentalsRentalID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentItemRental", x => new { x.EquipmentItemsEquipmentItemID, x.RentalsRentalID });
                    table.ForeignKey(
                        name: "FK_EquipmentItemRental_EquipmentItems_EquipmentItemsEquipmentItemID",
                        column: x => x.EquipmentItemsEquipmentItemID,
                        principalTable: "EquipmentItems",
                        principalColumn: "EquipmentItemID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentItemRental_Rentals_RentalsRentalID",
                        column: x => x.RentalsRentalID,
                        principalTable: "Rentals",
                        principalColumn: "RentalID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentItemRental_RentalsRentalID",
                table: "EquipmentItemRental",
                column: "RentalsRentalID");

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_SeasonPrices_SeasonPriceID",
                table: "Accommodations",
                column: "SeasonPriceID",
                principalTable: "SeasonPrices",
                principalColumn: "SeasonPriceID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
