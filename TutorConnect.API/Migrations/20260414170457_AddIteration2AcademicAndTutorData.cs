using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIteration2AcademicAndTutorData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Assignment_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Assignment_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Assignment_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Module_Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Code1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Assignment_ID);
                    table.ForeignKey(
                        name: "FK_Assignments_Modules_Module_Code1",
                        column: x => x.Module_Code1,
                        principalTable: "Modules",
                        principalColumn: "Module_Code");
                });

            migrationBuilder.CreateTable(
                name: "Log_Hours",
                columns: table => new
                {
                    Log_Hours_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Log_Hours_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Log_Hours_Time = table.Column<TimeOnly>(type: "time", nullable: false),
                    Log_Hours_Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tutor_ID = table.Column<int>(type: "int", nullable: false),
                    TutorUser_ID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log_Hours", x => x.Log_Hours_ID);
                    table.ForeignKey(
                        name: "FK_Log_Hours_Users_TutorUser_ID",
                        column: x => x.TutorUser_ID,
                        principalTable: "Users",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateTable(
                name: "Module_Resources",
                columns: table => new
                {
                    Module_Resource_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Module_Resource_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Resource_Type_ID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Code1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Module_Resources", x => x.Module_Resource_ID);
                    table.ForeignKey(
                        name: "FK_Module_Resources_Modules_Module_Code1",
                        column: x => x.Module_Code1,
                        principalTable: "Modules",
                        principalColumn: "Module_Code");
                });

            migrationBuilder.CreateTable(
                name: "Quizzes",
                columns: table => new
                {
                    Quiz_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Quiz_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quiz_Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quiz_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Start_Time = table.Column<TimeSpan>(type: "time", nullable: false),
                    End_Time = table.Column<TimeSpan>(type: "time", nullable: false),
                    Module_Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module_Code1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzes", x => x.Quiz_ID);
                    table.ForeignKey(
                        name: "FK_Quizzes_Modules_Module_Code1",
                        column: x => x.Module_Code1,
                        principalTable: "Modules",
                        principalColumn: "Module_Code");
                });

            migrationBuilder.CreateTable(
                name: "Assignment_Submissions",
                columns: table => new
                {
                    Assignment_Submission_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Assignment_grade_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Assignment_Feedback = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Assignment_ID = table.Column<int>(type: "int", nullable: false),
                    Assignment_ID1 = table.Column<int>(type: "int", nullable: true),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    StudentUser_ID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignment_Submissions", x => x.Assignment_Submission_ID);
                    table.ForeignKey(
                        name: "FK_Assignment_Submissions_Assignments_Assignment_ID1",
                        column: x => x.Assignment_ID1,
                        principalTable: "Assignments",
                        principalColumn: "Assignment_ID");
                    table.ForeignKey(
                        name: "FK_Assignment_Submissions_Users_StudentUser_ID",
                        column: x => x.StudentUser_ID,
                        principalTable: "Users",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_Submissions_Assignment_ID1",
                table: "Assignment_Submissions",
                column: "Assignment_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_Submissions_StudentUser_ID",
                table: "Assignment_Submissions",
                column: "StudentUser_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_Module_Code1",
                table: "Assignments",
                column: "Module_Code1");

            migrationBuilder.CreateIndex(
                name: "IX_Log_Hours_TutorUser_ID",
                table: "Log_Hours",
                column: "TutorUser_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Module_Resources_Module_Code1",
                table: "Module_Resources",
                column: "Module_Code1");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_Module_Code1",
                table: "Quizzes",
                column: "Module_Code1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignment_Submissions");

            migrationBuilder.DropTable(
                name: "Log_Hours");

            migrationBuilder.DropTable(
                name: "Module_Resources");

            migrationBuilder.DropTable(
                name: "Quizzes");

            migrationBuilder.DropTable(
                name: "Assignments");
        }
    }
}
