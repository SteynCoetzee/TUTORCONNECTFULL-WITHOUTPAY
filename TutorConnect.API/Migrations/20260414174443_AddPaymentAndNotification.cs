using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAndNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Notification_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date_Sent = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Is_Read = table.Column<bool>(type: "bit", nullable: false),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    User_ID1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Notification_ID);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_User_ID1",
                        column: x => x.User_ID1,
                        principalTable: "Users",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Payment_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Payment_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Payment_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Account_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Account_Number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Branch_Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bank = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Payment_Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    StudentUser_ID = table.Column<int>(type: "int", nullable: true),
                    Module_Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Code1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Payment_ID);
                    table.ForeignKey(
                        name: "FK_Payments_Modules_Module_Code1",
                        column: x => x.Module_Code1,
                        principalTable: "Modules",
                        principalColumn: "Module_Code");
                    table.ForeignKey(
                        name: "FK_Payments_Users_StudentUser_ID",
                        column: x => x.StudentUser_ID,
                        principalTable: "Users",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_ID1",
                table: "Notifications",
                column: "User_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Module_Code1",
                table: "Payments",
                column: "Module_Code1");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_StudentUser_ID",
                table: "Payments",
                column: "StudentUser_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
