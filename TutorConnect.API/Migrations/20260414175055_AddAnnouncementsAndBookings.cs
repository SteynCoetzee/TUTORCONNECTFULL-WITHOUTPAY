using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementsAndBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Announcement_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Announcement_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Announcement_Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tutor_ID = table.Column<int>(type: "int", nullable: true),
                    Admin_ID = table.Column<int>(type: "int", nullable: true),
                    Module_Code = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Announcement_ID);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Booking_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    StudentUser_ID = table.Column<int>(type: "int", nullable: true),
                    Booking_Slot_ID = table.Column<int>(type: "int", nullable: false),
                    Booking_Slot_ID1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Booking_ID);
                    table.ForeignKey(
                        name: "FK_Bookings_Booking_Slots_Booking_Slot_ID1",
                        column: x => x.Booking_Slot_ID1,
                        principalTable: "Booking_Slots",
                        principalColumn: "Booking_Slot_ID");
                    table.ForeignKey(
                        name: "FK_Bookings_Users_StudentUser_ID",
                        column: x => x.StudentUser_ID,
                        principalTable: "Users",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Booking_Slot_ID1",
                table: "Bookings",
                column: "Booking_Slot_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StudentUser_ID",
                table: "Bookings",
                column: "StudentUser_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "Bookings");
        }
    }
}
