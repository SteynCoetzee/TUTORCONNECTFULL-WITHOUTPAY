using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalIteration2Backbone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Audit_Logs",
                columns: table => new
                {
                    Audit_Log_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Audit_Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Audit_Time = table.Column<TimeOnly>(type: "time", nullable: false),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    Transaction_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Critical_Data = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audit_Logs", x => x.Audit_Log_ID);
                });

            migrationBuilder.CreateTable(
                name: "FAQ_Categories",
                columns: table => new
                {
                    FAQ_Category_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQ_Categories", x => x.FAQ_Category_ID);
                });

            migrationBuilder.CreateTable(
                name: "FAQs",
                columns: table => new
                {
                    FAQ_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FAQ_Category_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FAQs", x => x.FAQ_ID);
                });

            migrationBuilder.CreateTable(
                name: "Help_Pages",
                columns: table => new
                {
                    Help_Page_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Help_Page_Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Help_Page_Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Help_Pages", x => x.Help_Page_ID);
                });

            migrationBuilder.CreateTable(
                name: "Help_Resources",
                columns: table => new
                {
                    Help_Video_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Video_URL = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Help_Resources", x => x.Help_Video_ID);
                });

            migrationBuilder.CreateTable(
                name: "Media_Contents",
                columns: table => new
                {
                    Media_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Media_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Media_Address = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Media_Contents", x => x.Media_ID);
                });

            migrationBuilder.CreateTable(
                name: "Session_Reviews",
                columns: table => new
                {
                    Session_Review_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Session_Rating = table.Column<int>(type: "int", nullable: false),
                    Session_Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Session_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session_Reviews", x => x.Session_Review_ID);
                });

            migrationBuilder.CreateTable(
                name: "Testimonial_Categories",
                columns: table => new
                {
                    Testimonial_Category_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Test_Category_Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Testimonial_Categories", x => x.Testimonial_Category_ID);
                });

            migrationBuilder.CreateTable(
                name: "Testimonials",
                columns: table => new
                {
                    Testimonial_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Testimonial_Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Testimonial_Category_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Testimonials", x => x.Testimonial_ID);
                });

            migrationBuilder.CreateTable(
                name: "Tutor_Reviews",
                columns: table => new
                {
                    Tutor_Review_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tutor_Rating = table.Column<int>(type: "int", nullable: false),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Tutor_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tutor_Reviews", x => x.Tutor_Review_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audit_Logs");

            migrationBuilder.DropTable(
                name: "FAQ_Categories");

            migrationBuilder.DropTable(
                name: "FAQs");

            migrationBuilder.DropTable(
                name: "Help_Pages");

            migrationBuilder.DropTable(
                name: "Help_Resources");

            migrationBuilder.DropTable(
                name: "Media_Contents");

            migrationBuilder.DropTable(
                name: "Session_Reviews");

            migrationBuilder.DropTable(
                name: "Testimonial_Categories");

            migrationBuilder.DropTable(
                name: "Testimonials");

            migrationBuilder.DropTable(
                name: "Tutor_Reviews");
        }
    }
}
