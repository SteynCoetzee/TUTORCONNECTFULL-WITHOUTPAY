using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalIteration2Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendance_Statuses",
                columns: table => new
                {
                    Attendance_Status_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance_Statuses", x => x.Attendance_Status_ID);
                });

            migrationBuilder.CreateTable(
                name: "Booking_Slots",
                columns: table => new
                {
                    Booking_Slot_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slot_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Slot_Time = table.Column<TimeOnly>(type: "time", nullable: false),
                    Session_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Booked = table.Column<bool>(type: "bit", nullable: false),
                    Tutor_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Booking_Slots", x => x.Booking_Slot_ID);
                });

            migrationBuilder.CreateTable(
                name: "Business_Rules",
                columns: table => new
                {
                    Rule_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rule_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rule_Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Business_Rules", x => x.Rule_ID);
                });

            migrationBuilder.CreateTable(
                name: "Recorded_Sessions",
                columns: table => new
                {
                    Recording_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Recording_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Recording_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recording_Filesize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recording_Filetype = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Session_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recorded_Sessions", x => x.Recording_ID);
                });

            migrationBuilder.CreateTable(
                name: "Resource_Types",
                columns: table => new
                {
                    Resource_Type_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resource_Types", x => x.Resource_Type_ID);
                });

            migrationBuilder.CreateTable(
                name: "Student_Groups",
                columns: table => new
                {
                    Student_Group_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Group_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Groups", x => x.Student_Group_ID);
                });

            migrationBuilder.CreateTable(
                name: "Student_Quizzes",
                columns: table => new
                {
                    Student_Quiz_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Quiz_Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Quiz_ID = table.Column<int>(type: "int", nullable: false),
                    Quiz_ID1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Quizzes", x => x.Student_Quiz_ID);
                    table.ForeignKey(
                        name: "FK_Student_Quizzes_Quizzes_Quiz_ID1",
                        column: x => x.Quiz_ID1,
                        principalTable: "Quizzes",
                        principalColumn: "Quiz_ID");
                });

            migrationBuilder.CreateTable(
                name: "Session_Attendances",
                columns: table => new
                {
                    Session_Attendance_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Session_ID = table.Column<int>(type: "int", nullable: false),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Attendance_Status_ID = table.Column<int>(type: "int", nullable: false),
                    Attendance_Status_ID1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session_Attendances", x => x.Session_Attendance_ID);
                    table.ForeignKey(
                        name: "FK_Session_Attendances_Attendance_Statuses_Attendance_Status_ID1",
                        column: x => x.Attendance_Status_ID1,
                        principalTable: "Attendance_Statuses",
                        principalColumn: "Attendance_Status_ID");
                });

            migrationBuilder.CreateTable(
                name: "Student_Group_Allocations",
                columns: table => new
                {
                    Allocation_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Student_Group_ID = table.Column<int>(type: "int", nullable: false),
                    Student_Group_ID1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Group_Allocations", x => x.Allocation_ID);
                    table.ForeignKey(
                        name: "FK_Student_Group_Allocations_Student_Groups_Student_Group_ID1",
                        column: x => x.Student_Group_ID1,
                        principalTable: "Student_Groups",
                        principalColumn: "Student_Group_ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Session_Attendances_Attendance_Status_ID1",
                table: "Session_Attendances",
                column: "Attendance_Status_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Group_Allocations_Student_Group_ID1",
                table: "Student_Group_Allocations",
                column: "Student_Group_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Quizzes_Quiz_ID1",
                table: "Student_Quizzes",
                column: "Quiz_ID1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Booking_Slots");

            migrationBuilder.DropTable(
                name: "Business_Rules");

            migrationBuilder.DropTable(
                name: "Recorded_Sessions");

            migrationBuilder.DropTable(
                name: "Resource_Types");

            migrationBuilder.DropTable(
                name: "Session_Attendances");

            migrationBuilder.DropTable(
                name: "Student_Group_Allocations");

            migrationBuilder.DropTable(
                name: "Student_Quizzes");

            migrationBuilder.DropTable(
                name: "Attendance_Statuses");

            migrationBuilder.DropTable(
                name: "Student_Groups");
        }
    }
}
