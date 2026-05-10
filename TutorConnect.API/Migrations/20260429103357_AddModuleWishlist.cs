using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleWishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Module_Wishlists",
                columns: table => new
                {
                    Wishlist_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Module_Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Date_Submitted = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Module_Wishlists", x => x.Wishlist_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Module_Wishlists");
        }
    }
}
