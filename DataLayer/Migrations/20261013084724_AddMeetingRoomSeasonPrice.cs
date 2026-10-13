using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingRoomSeasonPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "MeetingRooms");

            migrationBuilder.AlterColumn<string>(
                name: "MeetingRoomNumber",
                table: "MeetingRooms",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "MeetingRoomSeasonPrice",
                columns: table => new
                {
                    MeetingRoomSeasonPriceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeekNumber = table.Column<int>(type: "int", nullable: false),
                    PriceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MeetingRoomID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRoomSeasonPrice", x => x.MeetingRoomSeasonPriceID);
                    table.ForeignKey(
                        name: "FK_MeetingRoomSeasonPrice_MeetingRooms_MeetingRoomID",
                        column: x => x.MeetingRoomID,
                        principalTable: "MeetingRooms",
                        principalColumn: "MeetingRoomID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRoomSeasonPrice_MeetingRoomID",
                table: "MeetingRoomSeasonPrice",
                column: "MeetingRoomID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingRoomSeasonPrice");

            migrationBuilder.AlterColumn<int>(
                name: "MeetingRoomNumber",
                table: "MeetingRooms",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "MeetingRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
