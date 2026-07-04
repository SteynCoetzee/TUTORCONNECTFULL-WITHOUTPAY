using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TutorConnect.API.Data;

#nullable disable

namespace TutorConnect.API.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260704000001_AddSessionTracking")]
    public partial class AddSessionTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add sessions remaining columns — default 5 for all existing rows
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Student_Modules]') AND name = 'Sessions_Remaining_Group')
                    ALTER TABLE [Student_Modules] ADD [Sessions_Remaining_Group] int NOT NULL DEFAULT 5;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Student_Modules]') AND name = 'Sessions_Remaining_OneOnOne')
                    ALTER TABLE [Student_Modules] ADD [Sessions_Remaining_OneOnOne] int NOT NULL DEFAULT 5;
            ");

            // For existing rows where a session type was disabled, set remaining to 0
            migrationBuilder.Sql(@"
                UPDATE [Student_Modules] SET [Sessions_Remaining_Group] = 0 WHERE [Can_Book_Group] = 0;
                UPDATE [Student_Modules] SET [Sessions_Remaining_OneOnOne] = 0 WHERE [Can_Book_OneOnOne] = 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Student_Modules]') AND name = 'Sessions_Remaining_Group')
                    ALTER TABLE [Student_Modules] DROP COLUMN [Sessions_Remaining_Group];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Student_Modules]') AND name = 'Sessions_Remaining_OneOnOne')
                    ALTER TABLE [Student_Modules] DROP COLUMN [Sessions_Remaining_OneOnOne];
            ");
        }
    }
}
