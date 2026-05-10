using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleCodeToBookingSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Module_Code",
                table: "Booking_Slots",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Module_Code",
                table: "Booking_Slots");
        }
    }
}
