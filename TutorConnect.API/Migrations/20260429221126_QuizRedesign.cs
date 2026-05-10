using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class QuizRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "End_Time",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Start_Time",
                table: "Quizzes");

            migrationBuilder.AddColumn<DateTime>(
                name: "End_Time",
                table: "Student_Quizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Start_Time",
                table: "Student_Quizzes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Quiz_Question_Options",
                columns: table => new
                {
                    Option_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question_ID = table.Column<int>(type: "int", nullable: false),
                    Option_Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Is_Correct = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quiz_Question_Options", x => x.Option_ID);
                });

            migrationBuilder.CreateTable(
                name: "Quiz_Questions",
                columns: table => new
                {
                    Question_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Quiz_ID = table.Column<int>(type: "int", nullable: false),
                    Question_Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Question_Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quiz_Questions", x => x.Question_ID);
                });

            migrationBuilder.CreateTable(
                name: "Student_Quiz_Answers",
                columns: table => new
                {
                    Answer_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Student_Quiz_ID = table.Column<int>(type: "int", nullable: false),
                    Question_ID = table.Column<int>(type: "int", nullable: false),
                    Option_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student_Quiz_Answers", x => x.Answer_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quiz_Question_Options");

            migrationBuilder.DropTable(
                name: "Quiz_Questions");

            migrationBuilder.DropTable(
                name: "Student_Quiz_Answers");

            migrationBuilder.DropColumn(
                name: "End_Time",
                table: "Student_Quizzes");

            migrationBuilder.DropColumn(
                name: "Start_Time",
                table: "Student_Quizzes");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "End_Time",
                table: "Quizzes",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "Start_Time",
                table: "Quizzes",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
