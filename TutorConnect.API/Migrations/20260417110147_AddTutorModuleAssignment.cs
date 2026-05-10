using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorModuleAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tutor_Modules",
                columns: table => new
                {
                    Tutor_Module_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tutor_ID = table.Column<int>(type: "int", nullable: false),
                    Module_Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Assigned_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tutor_Modules", x => x.Tutor_Module_ID);
                    table.ForeignKey(
                        name: "FK_Tutor_Modules_Modules_Module_Code",
                        column: x => x.Module_Code,
                        principalTable: "Modules",
                        principalColumn: "Module_Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tutor_Modules_Users_Tutor_ID",
                        column: x => x.Tutor_ID,
                        principalTable: "Users",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Modules_Module_Code",
                table: "Tutor_Modules",
                column: "Module_Code");

            migrationBuilder.CreateIndex(
                name: "IX_Tutor_Modules_Tutor_ID",
                table: "Tutor_Modules",
                column: "Tutor_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tutor_Modules");
        }
    }
}
